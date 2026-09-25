using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using backend.Data;
using backend.DTOs;
using backend.Models;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

    private static readonly string[] CurtainStages = ["Product ordered", "Product dispatched", "Product received by tailor", "Product dispatched by tailor", "Received by us", "Delivered to customer"];
    private static readonly string[] DefaultStages = ["Product ordered", "Product dispatched", "Product received by us", "Delivered to customer"];

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? serial, [FromQuery] string? company,
        [FromQuery] string? productType, [FromQuery] string? quoteStatus,
        [FromQuery] string? paymentStatus)
    {
        var query = _db.Products.Include(p => p.Customer).Include(p => p.WorkStages).AsQueryable();
        if (!string.IsNullOrWhiteSpace(serial))
            query = query.Where(p => p.SerialNumber.ToLower().Contains(serial.ToLower()));
        if (!string.IsNullOrWhiteSpace(company))
            query = query.Where(p => p.Company.ToLower().Contains(company.ToLower()));
        if (!string.IsNullOrWhiteSpace(productType))
        {
            if (productType == "curtain") query = query.Where(p => p.ProductType == "Curtain Fabric");
            else if (productType == "non-curtain") query = query.Where(p => p.ProductType != "Curtain Fabric");
            else query = query.Where(p => p.ProductType.ToLower().Contains(productType.ToLower()));
        }
        if (!string.IsNullOrWhiteSpace(quoteStatus))
            query = query.Where(p => p.QuotationStatus == quoteStatus);
        if (!string.IsNullOrWhiteSpace(paymentStatus))
            query = query.Where(p => p.PaymentStatus == paymentStatus);

        var products = await query.OrderByDescending(p => p.Id).ToListAsync();
        return Ok(products.Select(CustomersController.MapProduct));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _db.Products.Include(p => p.Customer).Include(p => p.WorkStages)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (p == null) return NotFound();
        return Ok(CustomersController.MapProduct(p));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
    {
        var product = new Product
        {
            CustomerId = dto.CustomerId, QuotationId = dto.QuotationId,
            ProductName = dto.ProductName,
            SerialNumber = dto.SerialNumber, Company = dto.Company,
            ProductType = dto.ProductType, Quantity = dto.Quantity,
            Colour = dto.Colour, Measurement = dto.Measurement,
            DateOrdered = dto.DateOrdered, ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            InternalRemarks = dto.InternalRemarks, QuotationAmount = dto.QuotationAmount,
            DiscountAmount = dto.DiscountAmount, QuotationStatus = dto.QuotationStatus,
            AdvancePayment = dto.AdvancePayment, PaymentStatus = dto.PaymentStatus,
            PaymentDate = dto.PaymentDate, PaymentRemarks = dto.PaymentRemarks
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var stages = product.ProductType == "Curtain Fabric" ? CurtainStages : DefaultStages;
        for (int i = 0; i < stages.Length; i++)
            _db.WorkStages.Add(new WorkStage { ProductId = product.Id, StageOrder = i + 1, StageName = stages[i] });
        await _db.SaveChangesAsync();

        var created = await _db.Products.Include(p => p.Customer).Include(p => p.WorkStages)
            .FirstAsync(p => p.Id == product.Id);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, CustomersController.MapProduct(created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductCreateDto dto)
    {
        var product = await _db.Products.Include(p => p.WorkStages).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        bool typeChanged = product.ProductType != dto.ProductType;
        product.ProductName = dto.ProductName; product.SerialNumber = dto.SerialNumber;
        product.Company = dto.Company; product.ProductType = dto.ProductType;
        product.Quantity = dto.Quantity; product.Colour = dto.Colour;
        product.Measurement = dto.Measurement; product.DateOrdered = dto.DateOrdered;
        product.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
        product.InternalRemarks = dto.InternalRemarks; product.QuotationAmount = dto.QuotationAmount;
        product.DiscountAmount = dto.DiscountAmount; product.QuotationStatus = dto.QuotationStatus;
        product.AdvancePayment = dto.AdvancePayment; product.PaymentStatus = dto.PaymentStatus;
        product.PaymentDate = dto.PaymentDate; product.PaymentRemarks = dto.PaymentRemarks;

        if (typeChanged)
        {
            _db.WorkStages.RemoveRange(product.WorkStages);
            var stages = product.ProductType == "Curtain Fabric" ? CurtainStages : DefaultStages;
            for (int i = 0; i < stages.Length; i++)
                _db.WorkStages.Add(new WorkStage { ProductId = product.Id, StageOrder = i + 1, StageName = stages[i] });
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{productId}/stages/{stageId}")]
    public async Task<IActionResult> UpdateStage(int productId, int stageId, [FromBody] WorkStageUpdateDto dto)
    {
        var stage = await _db.WorkStages.FirstOrDefaultAsync(s => s.Id == stageId && s.ProductId == productId);
        if (stage == null) return NotFound();
        stage.IsCompleted = dto.IsCompleted;
        stage.DateCompleted = dto.IsCompleted ? (dto.DateCompleted ?? DateTime.UtcNow) : null;
        stage.Remarks = dto.Remarks;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel()
    {
        var products = await _db.Products.Include(p => p.Customer).Include(p => p.WorkStages).ToListAsync();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Products");
        var headers = new[] { "Customer", "Product", "Serial No", "Company", "Type", "Qty", "Colour", "Measurement", "Date Ordered", "Expected Delivery", "Quote Amount", "Discount", "Final Amount", "Quote Status", "Advance", "Balance", "Payment Status" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;
        int row = 2;
        foreach (var p in products)
        {
            var final = p.QuotationAmount - p.DiscountAmount;
            ws.Cell(row, 1).Value = p.Customer?.Name ?? "";
            ws.Cell(row, 2).Value = p.ProductName;
            ws.Cell(row, 3).Value = p.SerialNumber;
            ws.Cell(row, 4).Value = p.Company;
            ws.Cell(row, 5).Value = p.ProductType;
            ws.Cell(row, 6).Value = p.Quantity;
            ws.Cell(row, 7).Value = p.Colour;
            ws.Cell(row, 8).Value = p.Measurement;
            ws.Cell(row, 9).Value = p.DateOrdered?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 10).Value = p.ExpectedDeliveryDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 11).Value = (double)p.QuotationAmount;
            ws.Cell(row, 12).Value = (double)p.DiscountAmount;
            ws.Cell(row, 13).Value = (double)final;
            ws.Cell(row, 14).Value = p.QuotationStatus;
            ws.Cell(row, 15).Value = (double)p.AdvancePayment;
            ws.Cell(row, 16).Value = (double)(final - p.AdvancePayment);
            ws.Cell(row, 17).Value = p.PaymentStatus;
            row++;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PFC_Products.xlsx");
    }
}
