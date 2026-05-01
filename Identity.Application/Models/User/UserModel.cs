using Shared.Domain.Enums;

namespace Identity.Application.Models.User
{
    public sealed record UserModel
    {
        public Guid Id { get; init; }
        public Guid TenantId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public List<string> Roles { get; init; } = new();
        public EntityStatusEnum Status { get; init; }
        public bool EmailConfirmed { get; init; }
        public int FailedLoginAttempts { get; init; }
        public DateTimeOffset? LastLoginDate { get; init; }
        public DateTimeOffset? LockoutEnd { get; init; }
        public DateTimeOffset CreatedOn { get; init; }
        public string CreatedByEmail { get; init; } = string.Empty;
        public string CreatedByName { get; init; } = string.Empty;
        public DateTimeOffset? ModifiedOn { get; init; }
        public string? ModifiedByEmail { get; init; }
        public string? ModifiedByName { get; init; }
    }
}
