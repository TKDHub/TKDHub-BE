using Dojo.Domain.Enums;

namespace Dojo.Application.Models.IncomeInvoice;

public sealed record CreateIncomeInvoiceModel
{
    public Guid                  StudentId { get; init; }
    public IncomeInvoiceTypeEnum Type      { get; init; }

    public decimal           OriginalPrice { get; init; }

    /// <summary>Percentage or Value. When percentage, the BE computes the JOD amount.</summary>
    public DiscountTypeEnum? DiscountType  { get; init; }

    /// <summary>Raw discount input: a percent (0–100) when DiscountType is Percentage, otherwise a flat amount.</summary>
    public decimal           DiscountValue { get; init; }

    /// <summary>
    /// How much is collected right now. Omit or 0 = nothing paid yet. The BE derives
    /// the payment status (Paid / PartiallyPaid / NotPaid) and the Open/Closed state
    /// from this against the computed price-after-discount — the client declares nothing.
    /// </summary>
    public decimal? AmountPaid { get; init; }

    /// <summary>Payment method for the first transaction. Required only when an amount is collected.</summary>
    public PaymentMethodEnum? PaymentMethod { get; init; }

    // ── Set by the controller from JWT claims ────────────────────
    public string CreatedByEmail { get; set; } = string.Empty;
    public string CreatedByName  { get; set; } = string.Empty;
}
