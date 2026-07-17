namespace Dojo.Application.Models.Class;

public sealed record AddStudentsToClassModel
{
    public Guid       ClassId    { get; set; }
    public List<Guid> StudentIds { get; init; } = [];

    // ── Audit (set by controller, not from request body) ─────────
    public string PerformedByEmail { get; set; } = string.Empty;
    public string PerformedByName  { get; set; } = string.Empty;
}
