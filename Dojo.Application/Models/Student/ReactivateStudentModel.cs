namespace Dojo.Application.Models.Student;

public sealed record ReactivateStudentModel
{
    public Guid     StudentId          { get; set; }
    public DateOnly StartDate          { get; init; }

    /// <summary>Only used when reactivating an Inactive (soft-deleted) student. Omit to keep their existing plan.</summary>
    public Guid?     SubscriptionPlanId { get; init; }

    // ── Audit (set by controller, not from request body) ─────────
    public string ModifiedByEmail { get; set; } = string.Empty;
    public string ModifiedByName  { get; set; } = string.Empty;
}
