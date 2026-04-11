using Identity.Domain.Enums;

namespace Identity.Application.Models.User
{
    public sealed record RegisterUserModel
    {
        public Guid TenantId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string ConfirmPassword { get; init; } = string.Empty;
        public AccountActorType? Actor { get; init; }
        public string PasswordHash { set; get; } = string.Empty;
    }
}
