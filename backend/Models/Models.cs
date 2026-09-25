using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class User
{
    public int Id { get; set; }
    [Required] public string Username { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
}

public class Customer
{
    public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public string AlternateContactNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Quotation> Quotations { get; set; } = new();
}

public class Quotation
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string QuotationNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Remarks { get; set; } = "";
    public decimal AdvancePayment { get; set; }
    public DateTime? AdvancePaymentDate { get; set; }
    public string DiscountType { get; set; } = "percent"; // "percent" or "flat"
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal FinalAmount { get; set; }
    public List<QuotationItem> Items { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<QuotationWindow> Windows { get; set; } = new();
}

public class QuotationItem
{
    public int Id { get; set; }
    public int QuotationId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Quotation? Quotation { get; set; }
    public int SrNo { get; set; }
    [Required] public string Particular { get; set; } = "";
    public string? Description { get; set; }
    public double Quantity { get; set; }
    public string Unit { get; set; } = "mtr";
    public decimal Rate { get; set; }
    public decimal Total { get; set; }
}

public class AppSetting
{
    public int Id { get; set; }
    [Required] public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SavedParticular
{
    public int Id { get; set; }
    [Required] public string Particular { get; set; } = "";
    public string DefaultUnit { get; set; } = "mtr";
    public decimal DefaultRate { get; set; }
    public int UsageCount { get; set; } = 1;
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}

public class QuotationWindow
{
    public int Id { get; set; }
    public int QuotationId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Quotation? Quotation { get; set; }
    [Required] public string Location { get; set; } = "";
    public string WindowName { get; set; } = "";
    [Required] public string TreatmentType { get; set; } = "Curtain";
    public double Width { get; set; }
    public double Height { get; set; }
    public int? Panels { get; set; }
    public bool IsDoubleCurtain { get; set; }
    public string? CurtainType { get; set; } = "Main";
    public double? SheerWidth { get; set; }
    public double? SheerHeight { get; set; }
    public int? SheerPanels { get; set; }
    public double Requirement { get; set; }
    public string Unit { get; set; } = "m";
    public string Remarks { get; set; } = "";
}

public class Product
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? QuotationId { get; set; }
    public Quotation? Quotation { get; set; }
    [Required] public string ProductName { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string Company { get; set; } = "";
    [Required] public string ProductType { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public string Colour { get; set; } = "";
    public string Measurement { get; set; } = "";
    public DateTime? DateOrdered { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string InternalRemarks { get; set; } = "";
    public decimal QuotationAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string QuotationStatus { get; set; } = "Pending";
    public decimal AdvancePayment { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime? PaymentDate { get; set; }
    public string PaymentRemarks { get; set; } = "";
    public List<WorkStage> WorkStages { get; set; } = new();
}

public class WorkStage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int StageOrder { get; set; }
    public string StageName { get; set; } = "";
    public bool IsCompleted { get; set; }
    public DateTime? DateCompleted { get; set; }
    public string Remarks { get; set; } = "";
}
