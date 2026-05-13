namespace Dojo.Application.Dtos.SubscriptionPlans;

public sealed class SubscriptionPlanDto
{
    public Guid     Id             { get; init; }
    public Guid     TenantId       { get; init; }
    public Guid     BranchId       { get; init; }
    public string   Name           { get; init; } = string.Empty;
    public string?  Description    { get; init; }
    public int      DurationMonths { get; init; }
    public decimal  Price          { get; init; }
    public string   Currency       { get; init; } = string.Empty;
    public string   Status         { get; init; } = string.Empty;
    public DateTimeOffset CreatedOn   { get; init; }
    public DateTimeOffset? ModifiedOn { get; init; }
}
