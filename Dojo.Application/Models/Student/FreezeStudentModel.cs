namespace Dojo.Application.Models.Student;

public sealed record FreezeStudentModel
{
    public Guid StudentId { get; set; }

    // ── Audit (set by controller, not from request body) ─────────
    public string FrozenByEmail { get; set; } = string.Empty;
    public string FrozenByName  { get; set; } = string.Empty;
}
