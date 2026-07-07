using Identity.Application.Services;

namespace Identity.Application.Tests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void HashPassword_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _sut.HashPassword("Str0ngP@ssword!");

        Assert.True(_sut.VerifyPassword("Str0ngP@ssword!", hash));
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        var hash = _sut.HashPassword("Str0ngP@ssword!");

        Assert.False(_sut.VerifyPassword("WrongPassword!", hash));
    }

    [Fact]
    public void HashPassword_NeverReturnsThePlainTextPassword()
    {
        var hash = _sut.HashPassword("Str0ngP@ssword!");

        Assert.NotEqual("Str0ngP@ssword!", hash);
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ProducesDifferentHashes()
    {
        // BCrypt salts each hash independently — this is what defeats rainbow-table attacks.
        var hash1 = _sut.HashPassword("Str0ngP@ssword!");
        var hash2 = _sut.HashPassword("Str0ngP@ssword!");

        Assert.NotEqual(hash1, hash2);
        Assert.True(_sut.VerifyPassword("Str0ngP@ssword!", hash1));
        Assert.True(_sut.VerifyPassword("Str0ngP@ssword!", hash2));
    }
}
