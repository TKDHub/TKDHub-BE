using Identity.Application.Dtos.Tenants;
using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Branches;
using Identity.Domain.Entities;

namespace Identity.Application.Mappings.Users
{
    public static class AuthMappings
    {
        public static AuthDto ToAuthDto(this User user, string? accessToken, string? refreshToken, DateTimeOffset expiresAt, TenantDto tenantDto)
        {
            return new AuthDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = user.UserRoles.Select(r => r.RoleId.ToString()).ToList(),
                Tenant = tenantDto,
                Branches = user.Branches?.ToListDtos() ?? new(),
                AccessToken = accessToken ?? string.Empty,
                RefreshToken = refreshToken ?? string.Empty,
                ExpiresAt = expiresAt,
            };
        }
    }
}
