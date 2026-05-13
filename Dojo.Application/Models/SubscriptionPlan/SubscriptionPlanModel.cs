namespace Dojo.Application.Models.SubscriptionPlan;

public sealed record SubscriptionPlanModel
{
    public Guid?   PlanId         { get; set; }
    public string  Name           { get; init; } = string.Empty;
    public string? Description    { get; init; }
    public int     DurationMonths { get; init; }
    public decimal Price          { get; init; }

    public string  CreatedByEmail  { get; set; } = string.Empty;
    public string  CreatedByName   { get; set; } = string.Empty;
    public string? ModifiedByEmail { get; set; }
    public string? ModifiedByName  { get; set; }
}
