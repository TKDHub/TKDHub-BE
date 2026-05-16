namespace Dojo.Application.Models.Student;

public sealed record StudentModel
{
    public Guid? StudentId { get; set; }

    // ── Identity ────────────────────────────────────────────────
    public string  FirstName   { get; init; } = string.Empty;
    public string  LastName    { get; init; } = string.Empty;
    public string? Email       { get; init; }
    public string  PhoneNumber { get; init; } = string.Empty;

    // ── Demographics ─────────────────────────────────────────────
    public DateOnly DateOfBirth { get; init; }
    public string   Gender      { get; init; } = string.Empty;

    // ── Membership ───────────────────────────────────────────────
    public DateOnly StartDate          { get; init; }
    public string   BeltLevel          { get; init; } = string.Empty;
    public Guid     SubscriptionPlanId { get; init; }

    // ── Optional ─────────────────────────────────────────────────
    public string? ProfileImage  { get; init; }
    public string? EmergencyContact { get; init; }

    // ── Snapshot (frozen at registration) ────────────────────────
    public decimal Price          { get; set; }
    public string  Currency       { get; set; } = string.Empty;
    public int     DurationMonths { get; set; }

    // ── Audit (set by controller, not from request body) ─────────
    public string  CreatedByEmail  { get; set; } = string.Empty;
    public string  CreatedByName   { get; set; } = string.Empty;
    public string? ModifiedByEmail { get; set; }
    public string? ModifiedByName  { get; set; }
}
