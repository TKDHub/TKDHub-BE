using Identity.Application.Dtos.Branches;
using Identity.Application.Dtos.Tenants;

namespace Identity.Application.Dtos.Users
{
    public sealed record RegisterDto
    {
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public TenantDto? Tenant { get; init; }
        public List<BranchDto> Branches { get; init; } = new();
        public string Message { get; init; } = string.Empty;
    }
}
