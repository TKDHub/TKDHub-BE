namespace Shared.Infrastructure.Authentication;

/// <summary>
/// Interface for JWT token generation
/// </summary>
public interface IJwtTokenGenerator
{
    string GenerateToken(Guid userId, string email, IEnumerable<string> roles);
}
