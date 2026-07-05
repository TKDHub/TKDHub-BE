using Shared.Domain.Primitives;

namespace Identity.Domain.Entities;

public sealed class User : AuditableEntity<Guid>
{
    [Searchable] public string Username { get; set; } = string.Empty;
    [Searchable] public string Email { get; set; } = string.Empty;
    // NOT [Searchable]: exposing this to dynamic filtering/sorting would let a client
    // extract the hash character-by-character via a StartsWith/ordering oracle.
    public string PasswordHash { get; set; } = string.Empty;
    [Searchable] public bool EmailConfirmed { get; set; }
    [Searchable] public DateTime? LastLoginDate { get; set; }
    [Searchable] public int FailedLoginAttempts { get; set; }
    [Searchable] public DateTime? LockoutEnd { get; set; }
    [Searchable] public string? PhoneNumber { get; set; }
    // NOT [Searchable]: secret tokens — same oracle-extraction risk as PasswordHash.
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiryTime { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
