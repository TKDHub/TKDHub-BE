namespace Dojo.Application.Dtos.IncomeInvoices;

public sealed class IncomeInvoiceDto
{
    public Guid    Id          { get; init; }
    public Guid    TenantId    { get; init; }
    public Guid    BranchId    { get; init; }
    public Guid    StudentId   { get; init; }
    public string? StudentName { get; init; }

    public string Type { get; init; } = string.Empty;

    // ── Frozen at creation ───────────────────────────────────────
    public decimal OriginalPrice { get; init; }
    public string? DiscountType  { get; init; }
    public decimal DiscountValue { get; init; }
    public string  Currency      { get; init; } = string.Empty;

    // ── Derived in the backend ───────────────────────────────────
    public decimal DiscountAmount     { get; init; }
    public decimal PriceAfterDiscount { get; init; }
    public decimal AmountPaid         { get; init; }
    public decimal RemainingAmount    { get; init; }

    /// <summary>Open / Closed lifecycle state, set by the backend.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Derived Paid / PartiallyPaid / NotPaid, computed from the transactions.</summary>
    public string PaymentStatus { get; init; } = string.Empty;

    public DateTimeOffset  CreatedOn  { get; init; }
    public DateTimeOffset? ModifiedOn { get; init; }

    public List<IncomeTransactionDto> Transactions { get; init; } = [];
}
