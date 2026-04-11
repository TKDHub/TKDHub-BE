using Identity.Application.Dtos.Users;
using Identity.Domain.Entities;

namespace Identity.Application.Mappings.Users
{
    public static class AuthMappings
    {
        /// <summary>
        /// Convert AuthenticationResponse to AuthDto
        /// </summary>
        public static AuthDto ToAuthDto(this User user, string? accessToken, string? refreshToken, DateTimeOffset expiresAt)
        {
            return new AuthDto
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AccessToken = accessToken ?? string.Empty,
                RefreshToken = refreshToken ?? string.Empty,
                ExpiresAt = expiresAt,
                Roles = user.Roles,
            };
        }
    }
}
