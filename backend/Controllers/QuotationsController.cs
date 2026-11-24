using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.DTOs;
using backend.Models;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public QuotationsController(AppDbContext db) => _db = db;

    [HttpGet("next-number")]
    public async Task<IActionResult> GetNextQuotationNumber()
    {
        var count = await _db.Quotations.CountAsync();
        var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
        var nextNumber = $"QT-{yearMonth}-{(count + 1):D3}";
        return Ok(new { quotationNumber = nextNumber });
    }

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var quotations = await _db.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Items)
            .Include(q => q.Products).ThenInclude(p => p.WorkStages)
            .Include(q => q.Windows)
            .Where(q => q.CustomerId == customerId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
        return Ok(quotations.Select(Map));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] string? search)
    {
        var query = _db.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Items)
            .Include(q => q.Products)
            .Include(q => q.Windows)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(q => q.Customer!.Name.ToLower().Contains(search.ToLower()) ||
                                     q.QuotationNumber.ToLower().Contains(search.ToLower()) ||
                                     q.Title.ToLower().Contains(search.ToLower()));

        var quotations = await query.OrderByDescending(q => q.CreatedAt).ToListAsync();

        var result = quotations.Select(q => new
        {
            q.Id,
            q.CustomerId,
            CustomerName = q.Customer!.Name,
            CustomerContactNumber = q.Customer?.ContactNumber ?? "",
            CustomerAddress = q.Customer?.Address ?? "",
            q.QuotationNumber,
            q.Title,
            q.CreatedAt,
            q.Remarks,
            q.DiscountType,
            q.DiscountValue,
            q.DiscountAmount,
            q.SubTotal,
            q.FinalAmount,
            Items = q.Items.OrderBy(i => i.SrNo).Select(MapItem).ToList(),
            Products = q.Products.Select(p => new
            {
                p.Id,
                p.ProductName,
                p.ProductType,
                FinalAmount = p.QuotationAmount - p.DiscountAmount,
                Balance = p.PaymentStatus == "Full payment cleared" ? 0 : (p.QuotationAmount - p.DiscountAmount - p.AdvancePayment),
                p.PaymentStatus,
                p.QuotationStatus
            }).ToList(),
            Windows = q.Windows.Select(w => CustomersController.MapWindow(w)).ToList(),
            TotalFinalAmount = q.Items.Any() ? q.FinalAmount : Math.Max(0, q.Products.Sum(p => p.QuotationAmount - p.DiscountAmount) - q.DiscountAmount),
            AdvancePayment = q.AdvancePayment,
            PaymentStatus = q.PaymentStatus,
            TotalAdvance = q.AdvancePayment,
            TotalBalance = q.PaymentStatus == "Full payment cleared" ? 0 : q.Items.Any() ? Math.Max(0, q.FinalAmount - q.AdvancePayment) : Math.Max(0, q.Products.Sum(p => Math.Max(0, p.QuotationAmount - p.DiscountAmount)) - q.DiscountAmount - q.AdvancePayment),
            QuotationStatus = !string.IsNullOrWhiteSpace(q.QuotationStatus) ? (q.QuotationStatus == "Approved" ? "Accepted" : q.QuotationStatus) : "Pending"
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var q = await _db.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Items)
            .Include(q => q.Products).ThenInclude(p => p.WorkStages)
            .Include(q => q.Windows)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (q == null) return NotFound();
        return Ok(Map(q));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QuotationCreateDto dto)
    {
        string quoteNo = dto.QuotationNumber?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(quoteNo))
        {
            var count = await _db.Quotations.CountAsync();
            quoteNo = $"QT-{DateTime.UtcNow:yyyyMM}-{(count + 1):D3}";
        }

        var quotation = new Quotation
        {
            CustomerId = dto.CustomerId,
            QuotationNumber = quoteNo,
            Title = dto.Title ?? "",
            Remarks = dto.Remarks ?? "",
            AdvancePayment = dto.AdvancePayment,
            PaymentStatus = dto.PaymentStatus == "Full payment cleared" ? dto.PaymentStatus : "Pending",
            AdvancePaymentDate = dto.AdvancePaymentDate,
            CreatedAt = dto.CreatedAt ?? DateTime.UtcNow,
            DiscountType = dto.DiscountType ?? "percent",
            DiscountValue = dto.DiscountValue,
            DiscountAmount = dto.DiscountAmount,
            SubTotal = dto.SubTotal,
            FinalAmount = dto.FinalAmount
        };

        if (dto.Items != null && dto.Items.Any())
        {
            int index = 1;
            foreach (var item in dto.Items)
            {
                var total = item.Total > 0 ? item.Total : ((decimal)item.Quantity * item.Rate);
                quotation.Items.Add(new QuotationItem
                {
                    SrNo = item.SrNo > 0 ? item.SrNo : index++,
                    Particular = item.Particular ?? "",
                    Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim(),
                    Quantity = item.Quantity,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "mtr" : item.Unit.Trim(),
                    Rate = item.Rate,
                    Total = total
                });
            }

            if (quotation.SubTotal == 0)
                quotation.SubTotal = quotation.Items.Sum(i => i.Total);
            if (quotation.FinalAmount == 0)
                quotation.FinalAmount = Math.Max(0, quotation.SubTotal - quotation.DiscountAmount);

            await AutoSaveParticularsAsync(dto.Items);
        }

        if (dto.Windows != null && dto.Windows.Any())
        {
            foreach (var w in dto.Windows)
            {
                quotation.Windows.Add(new QuotationWindow
                {
                    Location = w.Location,
                    WindowName = w.WindowName ?? "",
                    TreatmentType = w.TreatmentType,
                    Width = w.Width,
                    Height = w.Height,
                    Panels = w.Panels,
                    IsDoubleCurtain = w.IsDoubleCurtain,
                    CurtainType = w.CurtainType,
                    SheerWidth = w.SheerWidth,
                    SheerHeight = w.SheerHeight,
                    SheerPanels = w.SheerPanels,
                    Requirement = w.Requirement,
                    Unit = w.Unit,
                    Remarks = w.Remarks
                });
            }
        }

        _db.Quotations.Add(quotation);
        await _db.SaveChangesAsync();

        var created = await _db.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Items)
            .Include(q => q.Products)
            .Include(q => q.Windows)
            .FirstAsync(q => q.Id == quotation.Id);
        return CreatedAtAction(nameof(GetById), new { id = quotation.Id }, Map(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] QuotationCreateDto dto)
    {
        var q = await _db.Quotations
            .Include(q => q.Items)
            .Include(q => q.Windows)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (q == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.QuotationNumber))
            q.QuotationNumber = dto.QuotationNumber;
        q.Title = dto.Title ?? "";
        q.Remarks = dto.Remarks ?? "";
        q.QuotationStatus = dto.QuotationStatus is "Accepted" or "Approved" or "Pending" or "Rejected"
            ? (dto.QuotationStatus == "Approved" ? "Accepted" : dto.QuotationStatus)
            : q.QuotationStatus;
        q.AdvancePayment = dto.AdvancePayment;
        if (dto.PaymentStatus == "Full payment cleared") q.PaymentStatus = dto.PaymentStatus;
        q.AdvancePaymentDate = dto.AdvancePaymentDate;
        if (dto.CreatedAt.HasValue) q.CreatedAt = dto.CreatedAt.Value;
        q.DiscountType = dto.DiscountType ?? "percent";
        q.DiscountValue = dto.DiscountValue;
        q.DiscountAmount = dto.DiscountAmount;
        q.SubTotal = dto.SubTotal;
        q.FinalAmount = dto.FinalAmount;

        if (dto.Items != null)
        {
            _db.QuotationItems.RemoveRange(q.Items);
            q.Items.Clear();
            int index = 1;
            foreach (var item in dto.Items)
            {
                var total = item.Total > 0 ? item.Total : ((decimal)item.Quantity * item.Rate);
                q.Items.Add(new QuotationItem
                {
                    QuotationId = q.Id,
                    SrNo = item.SrNo > 0 ? item.SrNo : index++,
                    Particular = item.Particular ?? "",
                    Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim(),
                    Quantity = item.Quantity,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "mtr" : item.Unit.Trim(),
                    Rate = item.Rate,
                    Total = total
                });
            }

            if (q.SubTotal == 0)
                q.SubTotal = q.Items.Sum(i => i.Total);
            if (q.FinalAmount == 0)
                q.FinalAmount = Math.Max(0, q.SubTotal - q.DiscountAmount);

            await AutoSaveParticularsAsync(dto.Items);
        }

        if (dto.Windows != null)
        {
            _db.QuotationWindows.RemoveRange(q.Windows);
            q.Windows.Clear();
            foreach (var w in dto.Windows)
            {
                q.Windows.Add(new QuotationWindow
                {
                    QuotationId = q.Id,
                    Location = w.Location,
                    WindowName = w.WindowName ?? "",
                    TreatmentType = w.TreatmentType,
                    Width = w.Width,
                    Height = w.Height,
                    Panels = w.Panels,
                    IsDoubleCurtain = w.IsDoubleCurtain,
                    CurtainType = w.CurtainType,
                    SheerWidth = w.SheerWidth,
                    SheerHeight = w.SheerHeight,
                    SheerPanels = w.SheerPanels,
                    Requirement = w.Requirement,
                    Unit = w.Unit,
                    Remarks = w.Remarks
                });
            }
        }

        await _db.SaveChangesAsync();
        await SyncQuotationProductsAsync(q.Id);
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] QuotationStatusUpdateDto dto)
    {
        if (dto.Status is not ("Accepted" or "Approved" or "Pending" or "Rejected")) return BadRequest("Invalid quotation status.");
        var q = await _db.Quotations.Include(x => x.Products).Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (q == null) return NotFound();
        q.QuotationStatus = dto.Status == "Approved" ? "Accepted" : dto.Status;
        if (q.QuotationStatus == "Accepted")
            foreach (var product in q.Products) product.QuotationStatus = "Accepted";
        await _db.SaveChangesAsync();
        await SyncQuotationProductsAsync(q.Id);
        return NoContent();
    }

    [HttpPatch("{id}/advance")]
    public async Task<IActionResult> UpdateAdvance(int id, [FromBody] QuotationAdvanceUpdateDto dto)
    {
        if (dto.Amount < 0) return BadRequest("Advance cannot be negative.");
        var q = await _db.Quotations.Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id);
        if (q == null) return NotFound();
        q.AdvancePayment = dto.Amount;
        q.AdvancePaymentDate = dto.Amount > 0 ? dto.Date ?? DateTime.UtcNow : null;
        foreach (var product in q.Products) product.AdvancePayment = dto.Amount;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/discount")]
    public async Task<IActionResult> UpdateDiscount(int id, [FromBody] QuotationAdvanceUpdateDto dto)
    {
        if (dto.Amount < 0) return BadRequest("Discount cannot be negative.");
        var q = await _db.Quotations.Include(x => x.Products).Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (q == null) return NotFound();
        var quotedAmount = q.SubTotal > 0 ? q.SubTotal : q.Items.Any()
            ? q.Items.Sum(item => item.Total)
            : q.Products.Sum(product => Math.Max(0, product.QuotationAmount - product.DiscountAmount));
        q.DiscountType = "flat";
        q.DiscountAmount = Math.Min(dto.Amount, quotedAmount);
        q.DiscountValue = q.DiscountAmount;
        q.FinalAmount = Math.Max(0, quotedAmount - q.DiscountAmount);
        await _db.SaveChangesAsync();
        await SyncQuotationProductsAsync(q.Id);
        return NoContent();
    }

    [HttpPatch("{id}/payment-status")]
    public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] QuotationPaymentStatusUpdateDto dto)
    {
        if (dto.Status is not ("Pending" or "Full payment cleared")) return BadRequest("Invalid payment status.");
        var q = await _db.Quotations.Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id);
        if (q == null) return NotFound();
        q.PaymentStatus = dto.Status;
        foreach (var product in q.Products) product.PaymentStatus = dto.Status;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var q = await _db.Quotations.FindAsync(id);
        if (q == null) return NotFound();
        _db.Quotations.Remove(q);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task AutoSaveParticularsAsync(IEnumerable<QuotationItemCreateDto>? items)
    {
        if (items == null) return;
        foreach (var item in items)
        {
            var particularName = item.Particular?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(particularName)) continue;

            var existing = await _db.SavedParticulars.FirstOrDefaultAsync(p => p.Particular.ToLower() == particularName.ToLower());
            if (existing != null)
            {
                existing.UsageCount++;
                existing.LastUsedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(item.Unit)) existing.DefaultUnit = item.Unit.Trim();
                if (item.Rate > 0) existing.DefaultRate = item.Rate;
            }
            else
            {
                _db.SavedParticulars.Add(new SavedParticular
                {
                    Particular = particularName,
                    DefaultUnit = string.IsNullOrWhiteSpace(item.Unit) ? "mtr" : item.Unit.Trim(),
                    DefaultRate = item.Rate,
                    UsageCount = 1,
                    LastUsedAt = DateTime.UtcNow
                });
            }
        }
    }

    private static QuotationItemDto MapItem(QuotationItem i) => new()
    {
        Id = i.Id,
        QuotationId = i.QuotationId,
        SrNo = i.SrNo,
        Particular = i.Particular,
        Description = i.Description,
        Quantity = i.Quantity,
        Unit = i.Unit,
        Rate = i.Rate,
        Total = i.Total
    };

    private static QuotationDto Map(Quotation q) => new()
    {
        Id = q.Id,
        CustomerId = q.CustomerId,
        CustomerName = q.Customer?.Name ?? "",
        CustomerContactNumber = q.Customer?.ContactNumber ?? "",
        CustomerAddress = q.Customer?.Address ?? "",
        QuotationNumber = q.QuotationNumber,
        Title = q.Title,
        CreatedAt = q.CreatedAt,
        Remarks = q.Remarks,
        QuotationStatus = q.QuotationStatus,
        AdvancePayment = q.AdvancePayment,
        PaymentStatus = q.PaymentStatus,
        AdvancePaymentDate = q.AdvancePaymentDate,
        DiscountType = q.DiscountType,
        DiscountValue = q.DiscountValue,
        DiscountAmount = q.DiscountAmount,
        SubTotal = q.SubTotal,
        FinalAmount = q.FinalAmount,
        Items = q.Items.OrderBy(i => i.SrNo).Select(MapItem).ToList(),
        Products = q.Products.Select(p => CustomersController.MapProduct(p)).ToList(),
        Windows = q.Windows.Select(w => CustomersController.MapWindow(w)).ToList()
    };

    private async Task SyncQuotationProductsAsync(int quotationId)
    {
        var q = await _db.Quotations.Include(x => x.Products).FirstAsync(x => x.Id == quotationId);
        foreach (var product in q.Products)
        {
            product.AdvancePayment = q.AdvancePayment;
            product.PaymentStatus = q.PaymentStatus;
            if (q.QuotationStatus == "Accepted") product.QuotationStatus = "Accepted";
        }
        await _db.SaveChangesAsync();
    }
}
