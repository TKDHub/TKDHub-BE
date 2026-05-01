using Identity.Domain.Enums;

namespace Identity.Application.Models.User
{
    public sealed record RegisterUserModel
    {
        public Guid TenantId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public string Password { get; init; } = string.Empty;
        public string ConfirmPassword { get; init; } = string.Empty;
        public List<UserRoleEnum> Roles { get; init; } = new();
        public List<Guid> BranchIds { get; init; } = new();
        public string PasswordHash { set; get; } = string.Empty;
    }
}
