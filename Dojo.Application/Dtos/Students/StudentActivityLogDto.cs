namespace Dojo.Application.Dtos.Students;

public sealed class StudentActivityLogDto
{
    public Guid   Id           { get; init; }
    public Guid   StudentId    { get; init; }
    public string ActivityType { get; init; } = string.Empty;
    public string Description  { get; init; } = string.Empty;

    public DateTimeOffset CreatedOn      { get; init; }
    public string         CreatedByEmail { get; init; } = string.Empty;
    public string         CreatedByName  { get; init; } = string.Empty;
}
