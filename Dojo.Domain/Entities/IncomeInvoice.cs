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
    [Searchable] public Guid BranchId { get; set; }
    [Searchable] public Guid StudentId { get; set; }

    [Searchable] public IncomeInvoiceTypeEnum Type { get; set; }

    // ── Frozen at creation (source of truth) ─────────────────────
    [Searchable] public decimal           OriginalPrice { get; set; }
    [Searchable] public DiscountTypeEnum? DiscountType  { get; set; }
    [Searchable] public decimal           DiscountValue { get; set; }
    [Searchable] public string            Currency      { get; set; } = string.Empty;

    [Searchable] public IncomeInvoiceStatusEnum Status { get; set; } = IncomeInvoiceStatusEnum.Open;

    // ── Void audit (set only when Status == Voided) ──────────────
    public DateTimeOffset? VoidedOn      { get; set; }
    public string?         VoidedByEmail { get; set; }
    public string?         VoidedByName  { get; set; }
    public string?         VoidReason    { get; set; }

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

    /// <summary>Net collected: Paid transactions minus Refund transactions.</summary>
    public decimal AmountPaid => Transactions is null
        ? 0m
        : Transactions.Sum(t => t.Status == IncomeTransactionStatusEnum.Refund ? -t.Amount : t.Amount);

    /// <summary>
    /// Amount still owed. Forced to zero once voided — a voided invoice is cancelled
    /// and is never chased for payment, regardless of the net amounts underneath it.
    /// </summary>
    public decimal RemainingAmount => Status == IncomeInvoiceStatusEnum.Voided
        ? 0m
        : Math.Max(PriceAfterDiscount - AmountPaid, 0m);

    /// <summary>
    /// Derived from how much has been collected — never declared by the client.
    /// Computed straight from AmountPaid vs PriceAfterDiscount (not RemainingAmount),
    /// so it stays meaningful even for a voided invoice (e.g. NotPaid once refunded).
    /// </summary>
    public PaymentStatusEnum PaymentStatus =>
        AmountPaid <= 0                    ? PaymentStatusEnum.NotPaid
        : AmountPaid >= PriceAfterDiscount ? PaymentStatusEnum.Paid
        : PaymentStatusEnum.PartiallyPaid;
}
