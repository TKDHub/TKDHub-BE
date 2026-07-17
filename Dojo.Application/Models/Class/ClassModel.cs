namespace Dojo.Application.Models.Class;

public sealed record ClassModel
{
    public Guid? ClassId { get; set; }

    public string          Name      { get; init; } = string.Empty;
    public TimeOnly        StartTime { get; init; }
    public TimeOnly        EndTime   { get; init; }
    public List<DayOfWeek> Weekdays  { get; init; } = [];

    /// <summary>Initial roster, honored on create only — the add/remove/move endpoints manage it afterwards.</summary>
    public List<Guid> StudentIds { get; init; } = [];

    // ── Audit (set by controller, not from request body) ─────────
    public string  CreatedByEmail  { get; set; } = string.Empty;
    public string  CreatedByName   { get; set; } = string.Empty;
    public string? ModifiedByEmail { get; set; }
    public string? ModifiedByName  { get; set; }
}
