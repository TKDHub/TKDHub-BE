using Identity.Domain.Entities;

namespace Identity.Application.Contracts
{
    public sealed record AuthenticationResponse(Guid UserId, string Username, string Email, string AccessToken, string RefreshToken, DateTime ExpiresAt);

    public interface IAuthenticationService
    {
        AuthenticationResponse GenerateToken(User user, Tenant? tenant);
        string GenerateRefreshToken();
        bool ValidateRefreshToken(string refreshToken);
    }
}
