using Shared.Domain.Primitives;

namespace Dojo.Domain.Entities;

/// <summary>
/// A recurring training session (name, time window, and the weekdays it runs on).
/// A student belongs to at most one class at a time — enrolling them elsewhere moves them.
/// Soft-deleted via StatusId; blocked while it still has active students.
/// </summary>
public sealed class Class : AuditableEntity<Guid>, IHasBranch
{
    [Searchable] public Guid     BranchId  { get; set; }
    [Searchable] public string   Name      { get; set; } = string.Empty;
    [Searchable] public TimeOnly StartTime { get; set; }
    [Searchable] public TimeOnly EndTime   { get; set; }

    // Not [Searchable] — a collection isn't a valid target for the scalar ApplyFilter/ApplySort mechanism.
    public List<DayOfWeek> Weekdays { get; set; } = [];

    // ── Relations ────────────────────────────────────────────────
    public ICollection<Student> Students { get; set; } = [];
}
