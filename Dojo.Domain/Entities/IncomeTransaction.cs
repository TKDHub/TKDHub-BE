using Dojo.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Domain.Entities;

/// <summary>
/// A payment recorded against an income invoice. Transactions are the source of
/// truth for how much has been collected — the invoice's paid/remaining figures
/// are derived by summing them.
/// </summary>
public sealed class IncomeTransaction : AuditableEntity<Guid>, IHasBranch
{
    public Guid BranchId        { get; set; }
    public Guid IncomeInvoiceId { get; set; }

    public decimal           Amount { get; set; }
    public PaymentMethodEnum Method { get; set; }

    /// <summary>Paid (default) or Refund. A Refund row is a separate transaction, never a mutation of the original.</summary>
    public IncomeTransactionStatusEnum Status { get; set; } = IncomeTransactionStatusEnum.Paid;

    /// <summary>Set only on a Refund transaction — the original Paid transaction it refunds.</summary>
    public Guid? RefundOfTransactionId { get; set; }

    public DateTimeOffset? RefundedOn        { get; set; }
    public string?         RefundedByEmail   { get; set; }
    public string?         RefundedByName    { get; set; }
    public string?         RefundReason      { get; set; }

    // ── Relations ────────────────────────────────────────────────
    public IncomeInvoice IncomeInvoice { get; set; } = null!;
}
