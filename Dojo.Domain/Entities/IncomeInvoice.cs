using Dojo.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Domain.Entities;

/// <summary>
/// A single income charge raised against a student (subscription, exam, or kit).
/// Immutable once created — only transactions are added underneath it and the
/// Open/Closed status flips. Money figures are never stored; they are derived
/// from <see cref="OriginalPrice"/>, the discount inputs, and the transactions.
/// </summary>
public sealed class IncomeInvoice : AuditableEntity<Guid>, IHasBranch
{
    public Guid BranchId { get; set; }
    public Guid StudentId { get; set; }

    public IncomeInvoiceTypeEnum Type { get; set; }

    // ── Frozen at creation (source of truth) ─────────────────────
    public decimal           OriginalPrice { get; set; }
    public DiscountTypeEnum? DiscountType  { get; set; }
    public decimal           DiscountValue { get; set; }
    public string            Currency      { get; set; } = string.Empty;

    public IncomeInvoiceStatusEnum Status { get; set; } = IncomeInvoiceStatusEnum.Open;

    // ── Relations ────────────────────────────────────────────────
    public Student                        Student      { get; set; } = null!;
    public ICollection<IncomeTransaction> Transactions { get; set; } = [];

    // ── Derived (never stored) ───────────────────────────────────
    public decimal DiscountAmount => DiscountType switch
    {
        DiscountTypeEnum.Percentage => Math.Round(OriginalPrice * DiscountValue / 100m, 2, MidpointRounding.AwayFromZero),
        DiscountTypeEnum.Value      => Math.Min(DiscountValue, OriginalPrice),
        _                           => 0m
    };

    public decimal PriceAfterDiscount => Math.Max(OriginalPrice - DiscountAmount, 0m);

    public decimal AmountPaid => Transactions?.Sum(t => t.Amount) ?? 0m;

    public decimal RemainingAmount => Math.Max(PriceAfterDiscount - AmountPaid, 0m);

    /// <summary>Derived from how much has been collected — never declared by the client.</summary>
    public PaymentStatusEnum PaymentStatus =>
        RemainingAmount <= 0 ? PaymentStatusEnum.Paid
        : AmountPaid    <= 0 ? PaymentStatusEnum.NotPaid
        : PaymentStatusEnum.PartiallyPaid;
}
