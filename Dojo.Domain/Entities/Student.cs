using Dojo.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Domain.Entities;

public sealed class Student : AuditableEntity<Guid>, IHasBranch
{
    [Searchable] public Guid BranchId { get; set; }

    // ── Identity ────────────────────────────────────────────────
    [Searchable] public string  FirstName    { get; set; } = string.Empty;
    [Searchable] public string  LastName     { get; set; } = string.Empty;
    [Searchable] public string? Email        { get; set; }
    [Searchable] public string  PhoneNumber  { get; set; } = string.Empty;

    // ── Demographics ─────────────────────────────────────────────
    [Searchable] public DateOnly    DateOfBirth { get; set; }
    [Searchable] public GenderEnum  Gender      { get; set; }

    // ── Membership ───────────────────────────────────────────────
    [Searchable] public DateOnly          StartDate          { get; set; }
    [Searchable] public DateOnly          EndDate            { get; set; }
    [Searchable] public BeltLevelEnum     BeltLevel          { get; set; }
    [Searchable] public Guid              SubscriptionPlanId { get; set; }

    // ── Class enrollment ────────────────────────────────────────────
    // A student belongs to at most one class at a time.
    [Searchable] public Guid? ClassId { get; set; }

    // ── Freeze audit (set only while Status == Frozen) ────────────
    // RemainingDurationDays snapshots how many days were left until EndDate at the moment of
    // freezing — the clock is paused there, ready to resume from wherever the student left off.
    public DateOnly? FrozenOn              { get; set; }
    public string?   FrozenByEmail         { get; set; }
    public string?   FrozenByName          { get; set; }
    public int?      RemainingDurationDays { get; set; }

    // ── Snapshot (frozen at registration) ────────────────────────
    [Searchable] public decimal Price          { get; set; }
    [Searchable] public string  Currency       { get; set; } = string.Empty;
    [Searchable] public int     DurationMonths { get; set; }

    // ── Optional ─────────────────────────────────────────────────
    // NOT [Searchable]: ProfileImageUrl (no use case) and EmergencyContact (third-party PII).
    public string? ProfileImageUrl { get; set; }
    public string? EmergencyContact { get; set; }

    // ── Computed ─────────────────────────────────────────────────
    public string FullName => $"{FirstName} {LastName}".Trim();

    // ── Relations ─────────────────────────────────────────────────
    public SubscriptionPlan  SubscriptionPlan   { get; set; } = null!;
    public Class?             Class              { get; set; }
}
