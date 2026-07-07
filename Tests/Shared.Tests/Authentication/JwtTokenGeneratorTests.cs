using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Authentication;

namespace Shared.Tests.Authentication;

public class JwtTokenGeneratorTests
{
    private static readonly JwtSettings Settings = new()
    {
        Secret = "super-secret-test-signing-key-at-least-32-bytes-long",
        Issuer = "TKDHub.Test",
        Audience = "TKDHub.Test.Audience",
        ExpiryMinutes = 60
    };

    private static JwtTokenGenerator CreateSut() => new(Options.Create(Settings));

    [Fact]
    public void GenerateToken_ProducesTokenSignedWithConfiguredSecret()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();

        var token = sut.GenerateToken(userId, "user@test.com", ["Admin"]);

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = Settings.Issuer,
            ValidAudience = Settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.Secret)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);

        // JwtSecurityTokenHandler remaps the "sub" claim to ClaimTypes.NameIdentifier on the
        // validated principal by default — this also proves the signature verified successfully.
        Assert.Equal(userId.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void GenerateToken_EmbedsEmailAndRoleClaims()
    {
        var sut = CreateSut();

        var token = sut.GenerateToken(Guid.NewGuid(), "coach@test.com", ["Coach", "Admin"]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("coach@test.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        var roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(["Coach", "Admin"], roles);
    }

    [Fact]
    public void GenerateToken_SetsIssuerAudienceAndExpiryFromSettings()
    {
        var sut = CreateSut();
        var before = DateTime.UtcNow;

        var token = sut.GenerateToken(Guid.NewGuid(), "user@test.com", []);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(Settings.Issuer, jwt.Issuer);
        Assert.Contains(Settings.Audience, jwt.Audiences);
        Assert.InRange(jwt.ValidTo, before.AddMinutes(Settings.ExpiryMinutes - 1), before.AddMinutes(Settings.ExpiryMinutes + 1));
    }

    [Fact]
    public void GenerateToken_TwoCallsForSameUser_ProduceDifferentJtiClaims()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();

        var token1 = new JwtSecurityTokenHandler().ReadJwtToken(sut.GenerateToken(userId, "user@test.com", []));
        var token2 = new JwtSecurityTokenHandler().ReadJwtToken(sut.GenerateToken(userId, "user@test.com", []));

        var jti1 = token1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = token2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        Assert.NotEqual(jti1, jti2);
    }

    [Fact]
    public void GenerateToken_TamperedSignature_FailsValidation()
    {
        var sut = CreateSut();
        var token = sut.GenerateToken(Guid.NewGuid(), "user@test.com", []);
        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = Settings.Issuer,
            ValidAudience = Settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.Secret)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true
        };

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(tampered, validationParameters, out _));
    }
}
