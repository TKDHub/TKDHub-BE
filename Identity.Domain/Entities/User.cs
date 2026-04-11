using Shared.Domain.Primitives;

namespace Identity.Domain.Entities;

public sealed class User : AuditableEntity<Guid>
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public string? PhoneNumber { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiryTime { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    private readonly List<string> _roles = new();
    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();

    public void AddRoleInternal(string role)
    {
        _roles.Add(role);
    }

    /// <summary>
    /// Remove role - only Application layer can call
    /// </summary>
    public void RemoveRoleInternal(string role)
    {
        _roles.Remove(role);
    }
}