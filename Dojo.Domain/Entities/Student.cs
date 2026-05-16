using Dojo.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Domain.Entities;

public sealed class Student : AuditableEntity<Guid>, IHasBranch
{
    public Guid BranchId { get; set; }

    // ── Identity ────────────────────────────────────────────────
    public string  FirstName    { get; set; } = string.Empty;
    public string  LastName     { get; set; } = string.Empty;
    public string? Email        { get; set; }
    public string  PhoneNumber  { get; set; } = string.Empty;

    // ── Demographics ─────────────────────────────────────────────
    public DateOnly    DateOfBirth { get; set; }
    public GenderEnum  Gender      { get; set; }

    // ── Membership ───────────────────────────────────────────────
    public DateOnly          StartDate          { get; set; }
    public BeltLevelEnum     BeltLevel          { get; set; }
    public Guid              SubscriptionPlanId { get; set; }

    // ── Snapshot (frozen at registration) ────────────────────────
    public decimal Price          { get; set; }
    public string  Currency       { get; set; } = string.Empty;
    public int     DurationMonths { get; set; }

    // ── Optional ─────────────────────────────────────────────────
    public string? ProfileImageUrl { get; set; }
    public string? EmergencyContact { get; set; }

    // ── Computed ─────────────────────────────────────────────────
    public string FullName => $"{FirstName} {LastName}".Trim();

    // ── Relations ─────────────────────────────────────────────────
    public SubscriptionPlan  SubscriptionPlan   { get; set; } = null!;
}
