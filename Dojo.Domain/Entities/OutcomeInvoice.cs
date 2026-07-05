using Shared.Domain.Primitives;

namespace Dojo.Domain.Entities;

/// <summary>
/// An outgoing expense recorded against a branch (rent, supplies, utilities, etc.).
/// Unlike income invoices, this has no payment/transaction lifecycle — it's a single
/// recorded outflow, optionally with a receipt/attachment. Soft-deleted via StatusId.
/// </summary>
public sealed class OutcomeInvoice : AuditableEntity<Guid>, IHasBranch
{
    [Searchable] public Guid BranchId { get; set; }

    [Searchable] public string  Title         { get; set; } = string.Empty;
    [Searchable] public decimal Amount        { get; set; }
    [Searchable] public string  Currency      { get; set; } = string.Empty; // snapshot from the branch at creation
    public string? AttachmentUrl { get; set; }
    [Searchable] public string? Note          { get; set; }
}
