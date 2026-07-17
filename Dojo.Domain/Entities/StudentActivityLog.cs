using Dojo.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Domain.Entities;

/// <summary>
/// Write-once audit trail entry for a Student lifecycle event. Who/when is carried entirely by
/// the inherited CreatedOn/CreatedByEmail/CreatedByName — a log entry is never modified.
/// </summary>
public sealed class StudentActivityLog : AuditableEntity<Guid>, IHasBranch
{
    [Searchable] public Guid                BranchId     { get; set; }
    [Searchable] public Guid                StudentId    { get; set; }
    [Searchable] public StudentActivityType ActivityType { get; set; }
    public string Description { get; set; } = string.Empty;

    public Student Student { get; set; } = null!;
}
