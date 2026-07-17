namespace Dojo.Application.Dtos.Classes;

/// <summary>Flattened report row: a student alongside their currently linked class.</summary>
public sealed class StudentClassSummaryDto
{
    public Guid    StudentId { get; init; }
    public string  Name      { get; init; } = string.Empty;
    public string? ClassName { get; init; }
    public string  BeltLevel { get; init; } = string.Empty;
    public int     Age       { get; init; }
}
