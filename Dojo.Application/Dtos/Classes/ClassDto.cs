using Dojo.Application.Dtos.Students;

namespace Dojo.Application.Dtos.Classes;

public sealed class ClassDto
{
    public Guid    Id       { get; init; }
    public Guid    TenantId { get; init; }
    public Guid    BranchId { get; init; }

    public string    Name      { get; init; } = string.Empty;
    public TimeOnly  StartTime { get; init; }
    public TimeOnly  EndTime   { get; init; }
    public List<int> Weekdays  { get; init; } = [];

    public int    StudentCount { get; init; }
    public string Status       { get; init; } = string.Empty;

    public DateTimeOffset  CreatedOn  { get; init; }
    public DateTimeOffset? ModifiedOn { get; init; }

    /// <summary>Populated only on GetById — students currently enrolled in this class.</summary>
    public List<StudentDto>? Students { get; init; }
}
