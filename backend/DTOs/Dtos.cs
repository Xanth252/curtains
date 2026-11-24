namespace backend.DTOs;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username);
public record UpdateAccountRequest(string CurrentPassword, string Username, string? NewPassword);

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public string AlternateContactNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int ProductCount { get; set; }
}

public class CustomerDetailDto : CustomerDto
{
    public List<QuotationDto> Quotations { get; set; } = new();
    public List<ProductDto> Products { get; set; } = new();
}

public class QuotationWindowDto
{
    public int Id { get; set; }
    public int QuotationId { get; set; }
    public string Location { get; set; } = "";
    public string WindowName { get; set; } = "";
    public string TreatmentType { get; set; } = "Curtain";
    public double Width { get; set; }
    public double Height { get; set; }
    public int? Panels { get; set; }
    public bool IsDoubleCurtain { get; set; }
    public string? CurtainType { get; set; }
    public double? SheerWidth { get; set; }
    public double? SheerHeight { get; set; }
    public int? SheerPanels { get; set; }
    public double Requirement { get; set; }
    public string Unit { get; set; } = "m";
    public string Remarks { get; set; } = "";
    public string QuotationStatus { get; set; } = "Pending";
}

public class QuotationWindowCreateDto
{
    public string Location { get; set; } = "";
    public string WindowName { get; set; } = "";
    public string TreatmentType { get; set; } = "Curtain";
    public double Width { get; set; }
    public double Height { get; set; }
    public int? Panels { get; set; }
    public bool IsDoubleCurtain { get; set; }
    public string? CurtainType { get; set; }
    public double? SheerWidth { get; set; }
    public double? SheerHeight { get; set; }
    public int? SheerPanels { get; set; }
    public double Requirement { get; set; }
    public string Unit { get; set; } = "m";
    public string Remarks { get; set; } = "";
    public string QuotationStatus { get; set; } = "Pending";
}

public class QuotationItemDto
{
    public int Id { get; set; }
    public int QuotationId { get; set; }
    public int SrNo { get; set; }
    public string Particular { get; set; } = "";
    public string? Description { get; set; }
    public double Quantity { get; set; }
    public string Unit { get; set; } = "mtr";
    public decimal Rate { get; set; }
    public decimal Total { get; set; }
}

public class QuotationItemCreateDto
{
    public int SrNo { get; set; }
    public string Particular { get; set; } = "";
    public string? Description { get; set; }
    public double Quantity { get; set; }
    public string Unit { get; set; } = "mtr";
    public decimal Rate { get; set; }
    public decimal Total { get; set; }
}

public class QuotationDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerContactNumber { get; set; } = "";
    public string CustomerAddress { get; set; } = "";
    public string QuotationNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Remarks { get; set; } = "";
    public decimal AdvancePayment { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime? AdvancePaymentDate { get; set; }
    public string DiscountType { get; set; } = "percent";
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal FinalAmount { get; set; }
    public List<QuotationItemDto> Items { get; set; } = new();
    public List<ProductDto> Products { get; set; } = new();
    public List<QuotationWindowDto> Windows { get; set; } = new();
    public decimal TotalFinalAmount => Items.Any() ? FinalAmount : Math.Max(0, Products.Sum(p => p.FinalAmount) - DiscountAmount);
    public decimal TotalProductAdvance => Products.Sum(p => p.AdvancePayment);
    public decimal TotalAdvance => AdvancePayment;
    public decimal TotalBalance => PaymentStatus == "Full payment cleared" ? 0 : Items.Any() ? Math.Max(0, FinalAmount - AdvancePayment) : Math.Max(0, Products.Sum(p => p.FinalAmount) - DiscountAmount - AdvancePayment);
    public string QuotationStatus { get; set; } = "Pending";
}

public class QuotationCreateDto
{
    public int CustomerId { get; set; }
    public string QuotationNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public string Remarks { get; set; } = "";
    public string QuotationStatus { get; set; } = "Pending";
    public decimal AdvancePayment { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime? AdvancePaymentDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string DiscountType { get; set; } = "percent";
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal FinalAmount { get; set; }
    public List<QuotationItemCreateDto>? Items { get; set; }
    public List<QuotationWindowCreateDto>? Windows { get; set; }
}

public class QuotationStatusUpdateDto { public string Status { get; set; } = "Pending"; }
public class QuotationAdvanceUpdateDto { public decimal Amount { get; set; } public DateTime? Date { get; set; } }
public class QuotationPaymentStatusUpdateDto { public string Status { get; set; } = "Pending"; }

public class SavedParticularDto
{
    public int Id { get; set; }
    public string Particular { get; set; } = "";
    public string DefaultUnit { get; set; } = "mtr";
    public decimal DefaultRate { get; set; }
    public int UsageCount { get; set; }
    public DateTime LastUsedAt { get; set; }
}

public class SavedParticularCreateDto
{
    public string Particular { get; set; } = "";
    public string DefaultUnit { get; set; } = "mtr";
    public decimal DefaultRate { get; set; }
}

public class BusinessProfileDto
{
    public string FirmName { get; set; } = "PUKALE FURNISHING CENTRE";
    public string Address { get; set; } = "PFC Commercial Complex, Station Road";
    public string ContactNumber { get; set; } = "+91 9876543210";
    public string Email { get; set; } = "contact@pukalefurnishing.com";
    public string GstNumber { get; set; } = "27AAAAA0000A1Z5";
    public string PanNumber { get; set; } = "AAAAA0000A";
    public string LogoUrl { get; set; } = "";
}

public class BankDetailsDto
{
    public string BankName { get; set; } = "State Bank of India";
    public string AccountName { get; set; } = "Pukale Furnishing Centre";
    public string AccountNumber { get; set; } = "123456789012";
    public string IfscCode { get; set; } = "SBIN0001234";
    public string Branch { get; set; } = "Main Branch";
    public string TermsAndConditions { get; set; } = "1. Quotation valid for 30 days from issue date.\n2. 50% advance along with confirmed purchase order.\n3. Delivery and installation within 7-10 working days after site measurement.\n4. GST and any additional customization charges as applicable.";
}

public class CustomerCreateDto
{
    public string Name { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public string AlternateContactNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class ProductDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int? QuotationId { get; set; }
    public string CustomerName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string Company { get; set; } = "";
    public string ProductType { get; set; } = "";
    public int Quantity { get; set; }
    public string Colour { get; set; } = "";
    public string Measurement { get; set; } = "";
    public DateTime? DateOrdered { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string InternalRemarks { get; set; } = "";
    public decimal QuotationAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount => QuotationAmount - DiscountAmount;
    public string QuotationStatus { get; set; } = "Pending";
    public decimal AdvancePayment { get; set; }
    public decimal BalanceAmount => PaymentStatus == "Full payment cleared" ? 0 : FinalAmount - AdvancePayment;
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime? PaymentDate { get; set; }
    public string PaymentRemarks { get; set; } = "";
    public List<WorkStageDto> WorkStages { get; set; } = new();
    public int CompletedStages { get; set; }
    public int TotalStages { get; set; }
}

public class ProductCreateDto
{
    public int CustomerId { get; set; }
    public int? QuotationId { get; set; }
    public string ProductName { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string Company { get; set; } = "";
    public string ProductType { get; set; } = "";
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
}

public class WorkStageDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int StageOrder { get; set; }
    public string StageName { get; set; } = "";
    public bool IsCompleted { get; set; }
    public DateTime? DateCompleted { get; set; }
    public string Remarks { get; set; } = "";
}

public class WorkStageUpdateDto
{
    public bool IsCompleted { get; set; }
    public DateTime? DateCompleted { get; set; }
    public string Remarks { get; set; } = "";
}

public class DashboardDto
{
    public int TotalQuotations { get; set; }
    public int ApprovedQuotations { get; set; }
    public int RejectedQuotations { get; set; }
    public int PendingQuotations { get; set; }
    public decimal TotalQuotedAmount { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal TotalFinalAmount { get; set; }
    public decimal TotalAdvanceReceived { get; set; }
    public decimal TotalPaymentsCleared { get; set; }
    public decimal TotalPendingPayments { get; set; }
}

public class MonthlyReportDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = "";
    public int TotalQuotations { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Pending { get; set; }
    public decimal TotalQuotedAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalFinalAmount { get; set; }
    public decimal TotalAdvance { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal TotalCleared { get; set; }
    public decimal TotalPending { get; set; }
    public int PaymentsClearedCount { get; set; }
    public int PaymentsPendingCount { get; set; }
}
