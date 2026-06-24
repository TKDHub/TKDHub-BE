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

    // ── Relations ────────────────────────────────────────────────
    public IncomeInvoice IncomeInvoice { get; set; } = null!;
}
