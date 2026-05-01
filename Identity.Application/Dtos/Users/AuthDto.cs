using Identity.Application.Dtos.Branches;
using Identity.Application.Dtos.Tenants;

namespace Identity.Application.Dtos.Users
{
    public sealed record AuthDto
    {
        public Guid UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public List<string> Roles { get; init; } = new();
        public TenantDto? Tenant { get; init; }
        public List<BranchDto> Branches { get; init; } = new();
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; init; }
    }
}
