namespace Identity.Application.Dtos.Users;

public sealed class NotificationTargetDto
{
    public Guid    Id          { get; init; }
    public string  Name        { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string  Role        { get; init; } = string.Empty;
}
