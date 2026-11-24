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
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? phone)
    {
        var query = _db.Customers
            .Include(c => c.Quotations).ThenInclude(q => q.Products)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));
        if (!string.IsNullOrWhiteSpace(phone))
            query = query.Where(c => c.ContactNumber.Contains(phone) || c.AlternateContactNumber.Contains(phone));

        var customers = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        return Ok(customers.Select(c => new CustomerDto
        {
            Id = c.Id, Name = c.Name, ContactNumber = c.ContactNumber,
            AlternateContactNumber = c.AlternateContactNumber, Address = c.Address,
            Notes = c.Notes, CreatedAt = c.CreatedAt,
            ProductCount = c.Quotations.Sum(q => q.Products.Count)
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _db.Customers
            .Include(c => c.Quotations).ThenInclude(q => q.Items)
            .Include(c => c.Quotations).ThenInclude(q => q.Products).ThenInclude(p => p.WorkStages)
            .Include(c => c.Quotations).ThenInclude(q => q.Windows)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (c == null) return NotFound();
        
        // Also include products directly associated with customer (not in quotations)
        var directProducts = await _db.Products
            .Where(p => p.CustomerId == id && p.QuotationId == null)
            .Include(p => p.WorkStages)
            .ToListAsync();
        
        return Ok(MapToDetail(c, directProducts));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
    {
        var customer = new Customer
        {
            Name = dto.Name, ContactNumber = dto.ContactNumber,
            AlternateContactNumber = dto.AlternateContactNumber,
            Address = dto.Address, Notes = dto.Notes
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, new CustomerDto
        {
            Id = customer.Id, Name = customer.Name, ContactNumber = customer.ContactNumber,
            AlternateContactNumber = customer.AlternateContactNumber, Address = customer.Address,
            Notes = customer.Notes, CreatedAt = customer.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerCreateDto dto)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound();
        customer.Name = dto.Name; customer.ContactNumber = dto.ContactNumber;
        customer.AlternateContactNumber = dto.AlternateContactNumber;
        customer.Address = dto.Address; customer.Notes = dto.Notes;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound();
        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static CustomerDetailDto MapToDetail(Customer c, List<Product>? directProducts = null) => new()
    {
        Id = c.Id, Name = c.Name, ContactNumber = c.ContactNumber,
        AlternateContactNumber = c.AlternateContactNumber, Address = c.Address,
        Notes = c.Notes, CreatedAt = c.CreatedAt,
        ProductCount = c.Quotations.Sum(q => q.Products.Count) + (directProducts?.Count ?? 0),
        Quotations = c.Quotations.OrderByDescending(q => q.CreatedAt).Select(q => new QuotationDto
        {
            Id = q.Id, CustomerId = q.CustomerId, CustomerName = c.Name,
            CustomerContactNumber = c.ContactNumber,
            CustomerAddress = c.Address,
            QuotationNumber = q.QuotationNumber,
            Title = q.Title, CreatedAt = q.CreatedAt, Remarks = q.Remarks,
            AdvancePayment = q.AdvancePayment, AdvancePaymentDate = q.AdvancePaymentDate,
            PaymentStatus = q.PaymentStatus,
            QuotationStatus = q.QuotationStatus == "Approved" ? "Accepted" : q.QuotationStatus,
            DiscountType = q.DiscountType,
            DiscountValue = q.DiscountValue,
            DiscountAmount = q.DiscountAmount,
            SubTotal = q.SubTotal,
            FinalAmount = q.FinalAmount,
            Items = q.Items.OrderBy(i => i.SrNo).Select(MapItem).ToList(),
            Products = q.Products.Select(p => MapProduct(p)).ToList(),
            Windows = q.Windows.Select(w => MapWindow(w)).ToList()
        }).ToList(),
        Products = c.Quotations.SelectMany(q => q.Products)
            .Concat(directProducts ?? new())
            .OrderByDescending(p => p.Id)
            .Select(p => MapProduct(p)).ToList()
    };

    public static QuotationItemDto MapItem(QuotationItem i) => new()
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

    public static QuotationWindowDto MapWindow(QuotationWindow w) => new()
    {
        Id = w.Id,
        QuotationId = w.QuotationId,
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
    };

    public static ProductDto MapProduct(Product p) => new()
    {
        Id = p.Id, CustomerId = p.CustomerId, QuotationId = p.QuotationId,
        CustomerName = p.Customer?.Name ?? "",
        ProductName = p.ProductName, SerialNumber = p.SerialNumber, Company = p.Company,
        ProductType = p.ProductType, Quantity = p.Quantity, Colour = p.Colour,
        Measurement = p.Measurement, DateOrdered = p.DateOrdered,
        ExpectedDeliveryDate = p.ExpectedDeliveryDate, InternalRemarks = p.InternalRemarks,
        QuotationAmount = p.QuotationAmount, DiscountAmount = p.DiscountAmount,
        QuotationStatus = p.QuotationStatus, AdvancePayment = p.AdvancePayment,
        PaymentStatus = p.PaymentStatus, PaymentDate = p.PaymentDate,
        PaymentRemarks = p.PaymentRemarks,
        WorkStages = p.WorkStages.OrderBy(s => s.StageOrder).Select(s => new WorkStageDto
        {
            Id = s.Id, ProductId = s.ProductId, StageOrder = s.StageOrder,
            StageName = s.StageName, IsCompleted = s.IsCompleted,
            DateCompleted = s.DateCompleted, Remarks = s.Remarks
        }).ToList(),
        CompletedStages = p.WorkStages.Count(s => s.IsCompleted),
        TotalStages = p.WorkStages.Count
    };
}
