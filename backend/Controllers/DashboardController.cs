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
        if (!list.Any()) return "Pending";
        if (list.All(p => p.QuotationStatus is "Approved" or "Accepted")) return "Accepted";
        if (list.All(p => p.QuotationStatus == "Rejected")) return "Rejected";
        return "Pending";
    }

    private static string EffectiveQuoteStatus(backend.Models.Quotation q)
    {
        if (!string.IsNullOrWhiteSpace(q.QuotationStatus)) return q.QuotationStatus == "Approved" ? "Accepted" : q.QuotationStatus;
        return "Pending";
    }

    private static string PayStatus(IEnumerable<backend.Models.Product> products)
    {
        var list = products.ToList();
        if (list.All(p => p.PaymentStatus == "Full payment cleared")) return "Full payment cleared";
        return "Pending";
    }

    private static decimal ProductQuotationBalance(backend.Models.Quotation q)
    {
        if (q.PaymentStatus == "Full payment cleared") return 0;
        var outstandingProducts = q.Products.Where(p => p.PaymentStatus != "Full payment cleared").ToList();
        if (outstandingProducts.Count == 0) return 0;
        var quoted = outstandingProducts.Sum(p => Math.Max(0, p.QuotationAmount - p.DiscountAmount));
        // Quote-level advance is copied to linked products for display; count it once.
        var advance = q.AdvancePayment > 0 ? q.AdvancePayment : outstandingProducts.Sum(p => p.AdvancePayment);
        return Math.Max(0, quoted - q.DiscountAmount - advance);
    }

    private static decimal ItemizedQuotationBalance(backend.Models.Quotation q)
    {
        if (q.PaymentStatus == "Full payment cleared") return 0;
        var final = q.FinalAmount > 0 ? q.FinalAmount : Math.Max(0, q.SubTotal - q.DiscountAmount);
        return Math.Max(0, final - q.AdvancePayment);
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var allQuotations = await _db.Quotations.Include(q => q.Products).Include(q => q.Items).ToListAsync();
        // Standalone products are not saved quotations and never contribute to accepted quote totals.

        // Only count amounts for Approved products (pending/rejected quotations excluded)
        var approvedQuotationProducts = allQuotations
            .Where(q => EffectiveQuoteStatus(q) == "Accepted")
            .SelectMany(q => q.Products)
            .Where(p => p.QuotationStatus is "Approved" or "Accepted")
            .ToList();
        var allApproved = approvedQuotationProducts;

        var totalFinal   = allApproved.Sum(p => p.QuotationAmount - p.DiscountAmount);
        var clearedProds = allApproved.Where(p => p.PaymentStatus == "Full payment cleared").ToList();
        var totalCleared = clearedProds.Sum(p => p.QuotationAmount - p.DiscountAmount);
        // Compute outstanding quote by quote so pending/rejected quotes can never leak
        // into the dashboard total and a quote-level advance cannot offset another quote.
        var acceptedProductBalance = allQuotations
            .Where(q => EffectiveQuoteStatus(q) == "Accepted" && !q.Items.Any())
            .Sum(ProductQuotationBalance);
        var approvedItemized = allQuotations.Where(q => q.Items.Any() && EffectiveQuoteStatus(q) == "Accepted").ToList();
        var approvedItemizedAmount = approvedItemized.Sum(q => q.FinalAmount);
        var itemizedAdvance = approvedItemized.Sum(q => Math.Min(q.AdvancePayment, q.FinalAmount));

        return Ok(new DashboardDto
        {
            TotalQuotations      = allQuotations.Count,
            ApprovedQuotations   = allQuotations.Count(q => EffectiveQuoteStatus(q) == "Accepted"),
            RejectedQuotations   = allQuotations.Count(q => EffectiveQuoteStatus(q) == "Rejected"),
            PendingQuotations    = allQuotations.Count(q => EffectiveQuoteStatus(q) == "Pending"),
            TotalQuotedAmount    = allApproved.Sum(p => p.QuotationAmount) + approvedItemized.Sum(q => q.SubTotal > 0 ? q.SubTotal : q.FinalAmount),
            TotalDiscountAmount  = allApproved.Sum(p => p.DiscountAmount) + approvedItemized.Sum(q => q.DiscountAmount),
            TotalFinalAmount     = totalFinal + approvedItemizedAmount,
            TotalAdvanceReceived = allApproved.Sum(p => p.AdvancePayment) + itemizedAdvance,
            TotalPaymentsCleared = totalCleared,
            TotalPendingPayments = acceptedProductBalance
                + approvedItemized.Sum(ItemizedQuotationBalance)
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
