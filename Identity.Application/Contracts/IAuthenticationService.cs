using Identity.Domain.Entities;

namespace Identity.Application.Contracts
{
    /// <summary>
    /// Authentication service interface for JWT token operations
    /// </summary>
    public sealed record AuthenticationResponse(Guid UserId, string Email, string FirstName, string LastName, string AccessToken, string RefreshToken, DateTime ExpiresAt);

    public interface IAuthenticationService
    {
        AuthenticationResponse GenerateToken(User user, Tenant? tenant);
        string GenerateRefreshToken();
        bool ValidateRefreshToken(string refreshToken);
    }
}
