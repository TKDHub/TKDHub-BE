using Shared.Domain.Primitives;

namespace Dojo.Domain.Entities;

public sealed class SubscriptionPlan : AuditableEntity<Guid>, IHasBranch
{
    public Guid    BranchId       { get; set; }
    public string  Name           { get; set; } = string.Empty;
    public string? Description    { get; set; }
    public int     DurationMonths { get; set; }
    public decimal Price          { get; set; }
}
