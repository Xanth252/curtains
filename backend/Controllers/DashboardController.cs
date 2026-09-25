using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.DTOs;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    // A "quotation" = one customer. Status is determined by majority:
    // Approved if ALL products approved, Rejected if ALL rejected, else Pending.
    // Payment cleared if ALL products cleared, else Pending.
    private static string QuoteStatus(IEnumerable<backend.Models.Product> products)
    {
        var list = products.ToList();
        if (list.All(p => p.QuotationStatus == "Approved")) return "Approved";
        if (list.All(p => p.QuotationStatus == "Rejected")) return "Rejected";
        return "Pending";
    }

    private static string PayStatus(IEnumerable<backend.Models.Product> products)
    {
        var list = products.ToList();
        if (list.All(p => p.PaymentStatus == "Full payment cleared")) return "Full payment cleared";
        return "Pending";
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var allQuotations = await _db.Quotations.Include(q => q.Products).ToListAsync();
        var directProducts = await _db.Products.Where(p => p.QuotationId == null).ToListAsync();

        // Only count amounts for Approved products (pending/rejected quotations excluded)
        var approvedQuotationProducts = allQuotations
            .SelectMany(q => q.Products)
            .Where(p => p.QuotationStatus == "Approved")
            .ToList();
        var approvedDirectProducts = directProducts
            .Where(p => p.QuotationStatus == "Approved")
            .ToList();
        var allApproved = approvedQuotationProducts.Concat(approvedDirectProducts).ToList();

        var totalFinal   = allApproved.Sum(p => p.QuotationAmount - p.DiscountAmount);
        var clearedProds = allApproved.Where(p => p.PaymentStatus == "Full payment cleared").ToList();
        var totalCleared = clearedProds.Sum(p => p.QuotationAmount - p.DiscountAmount);
        var totalPending = allApproved
            .Where(p => p.PaymentStatus != "Full payment cleared")
            .Sum(p => p.QuotationAmount - p.DiscountAmount - p.AdvancePayment)
            - allQuotations
                .Where(q => QuoteStatus(q.Products) == "Approved")
                .Sum(q => q.AdvancePayment);

        return Ok(new DashboardDto
        {
            TotalQuotations      = allQuotations.Count,
            ApprovedQuotations   = allQuotations.Count(q => QuoteStatus(q.Products) == "Approved"),
            RejectedQuotations   = allQuotations.Count(q => QuoteStatus(q.Products) == "Rejected"),
            PendingQuotations    = allQuotations.Count(q => QuoteStatus(q.Products) == "Pending"),
            TotalQuotedAmount    = allApproved.Sum(p => p.QuotationAmount),
            TotalDiscountAmount  = allApproved.Sum(p => p.DiscountAmount),
            TotalFinalAmount     = totalFinal,
            TotalAdvanceReceived = allApproved.Sum(p => p.AdvancePayment),
            TotalPaymentsCleared = totalCleared,
            TotalPendingPayments = Math.Max(totalPending, 0)
        });
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthly([FromQuery] int? year)
    {
        var allQuotations = await _db.Quotations.Include(q => q.Products).ToListAsync();
        int targetYear = year ?? DateTime.UtcNow.Year;

        var grouped = allQuotations
            .GroupBy(q => new { Month = q.CreatedAt.Month, Year = q.CreatedAt.Year })
            .Where(g => g.Key.Year == targetYear)
            .OrderBy(g => g.Key.Month)
            .Select(monthGroup =>
            {
                var quotes   = monthGroup.ToList();
                // Only approved products count toward amounts
                var approved = quotes.SelectMany(q => q.Products)
                                     .Where(p => p.QuotationStatus == "Approved").ToList();

                var final   = approved.Sum(p => p.QuotationAmount - p.DiscountAmount);
                var advance = approved.Sum(p => p.AdvancePayment)
                            + quotes.Where(q => QuoteStatus(q.Products) == "Approved").Sum(q => q.AdvancePayment);

                int clearedCount = quotes.Count(q => PayStatus(q.Products) == "Full payment cleared");
                int pendingCount = quotes.Count(q => PayStatus(q.Products) == "Pending");

                decimal totalCleared = quotes
                    .Where(q => PayStatus(q.Products) == "Full payment cleared")
                    .SelectMany(q => q.Products)
                    .Where(p => p.QuotationStatus == "Approved")
                    .Sum(p => p.QuotationAmount - p.DiscountAmount);

                decimal totalPending = quotes
                    .Where(q => PayStatus(q.Products) == "Pending")
                    .SelectMany(q => q.Products)
                    .Where(p => p.QuotationStatus == "Approved")
                    .Sum(p => p.QuotationAmount - p.DiscountAmount - p.AdvancePayment)
                    - quotes.Where(q => QuoteStatus(q.Products) == "Approved").Sum(q => q.AdvancePayment);

                return new MonthlyReportDto
                {
                    Month             = monthGroup.Key.Month,
                    Year              = monthGroup.Key.Year,
                    MonthName         = new DateTime(monthGroup.Key.Year, monthGroup.Key.Month, 1).ToString("MMMM yyyy"),
                    TotalQuotations   = quotes.Count,
                    Approved          = quotes.Count(q => QuoteStatus(q.Products) == "Approved"),
                    Rejected          = quotes.Count(q => QuoteStatus(q.Products) == "Rejected"),
                    Pending           = quotes.Count(q => QuoteStatus(q.Products) == "Pending"),
                    TotalQuotedAmount = approved.Sum(p => p.QuotationAmount),
                    TotalDiscount     = approved.Sum(p => p.DiscountAmount),
                    TotalFinalAmount  = final,
                    TotalAdvance      = advance,
                    TotalBalance      = final - advance,
                    TotalCleared      = totalCleared,
                    TotalPending      = totalPending,
                    PaymentsClearedCount = clearedCount,
                    PaymentsPendingCount = pendingCount
                };
            })
            .ToList();

        var years = allQuotations
            .Select(q => q.CreatedAt.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();

        if (!years.Contains(DateTime.UtcNow.Year)) years.Insert(0, DateTime.UtcNow.Year);

        return Ok(new { months = grouped, years });
    }
}
