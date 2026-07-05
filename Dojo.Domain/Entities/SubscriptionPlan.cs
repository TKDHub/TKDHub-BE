using Shared.Domain.Primitives;

namespace Dojo.Domain.Entities;

public sealed class SubscriptionPlan : AuditableEntity<Guid>, IHasBranch
{
    [Searchable] public Guid    BranchId       { get; set; }
    [Searchable] public string  Name           { get; set; } = string.Empty;
    [Searchable] public string? Description    { get; set; }
    [Searchable] public int     DurationMonths { get; set; }
    [Searchable] public decimal Price          { get; set; }

    // ── Relations ─────────────────────────────────────────────────
    public ICollection<Student> Students { get; set; } = [];
}
