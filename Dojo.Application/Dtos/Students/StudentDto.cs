namespace Dojo.Application.Dtos.Students;

public sealed class StudentDto
{
    public Guid    Id       { get; init; }
    public Guid    TenantId { get; init; }
    public Guid    BranchId { get; init; }

    // ── Identity ────────────────────────────────────────────────
    public string  FirstName   { get; init; } = string.Empty;
    public string  LastName    { get; init; } = string.Empty;
    public string  FullName    { get; init; } = string.Empty;
    public string? Email       { get; init; }
    public string  PhoneNumber { get; init; } = string.Empty;

    // ── Demographics ─────────────────────────────────────────────
    public DateOnly DateOfBirth { get; init; }
    public string   Gender      { get; init; } = string.Empty;

    // ── Membership ───────────────────────────────────────────────
    public DateOnly StartDate              { get; init; }
    /// <summary>StartDate + DurationMonths, computed and persisted at registration/update time.</summary>
    public DateOnly EndDate                { get; init; }
    public string   BeltLevel              { get; init; } = string.Empty;
    public Guid     SubscriptionPlanId     { get; init; }
    public string   SubscriptionPlanName   { get; init; } = string.Empty;

    // ── Snapshot (frozen at registration) ────────────────────────
    public decimal  Price          { get; init; }
    public string   Currency       { get; init; } = string.Empty;
    public int      DurationMonths { get; init; }

    // ── Optional ─────────────────────────────────────────────────
    public string? ProfileImageUrl { get; init; }
    public string? EmergencyContact { get; init; }

    // ── Status ───────────────────────────────────────────────────
    public string Status { get; init; } = string.Empty;

    // ── Freeze audit (populated only when Status == Frozen) ──────
    public DateOnly? FrozenOn              { get; init; }
    public string?   FrozenByEmail         { get; init; }
    public string?   FrozenByName          { get; init; }
    public int?      RemainingDurationDays { get; init; }
}
