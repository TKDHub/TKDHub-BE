namespace Shared.Application.Models;

/// <summary>A person to notify — resolved from Identity's Admin/SuperAdmin roster.</summary>
public sealed class NotificationTarget
{
    public string  Name        { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string  Role        { get; init; } = string.Empty;
}
