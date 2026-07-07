using FluentValidation.TestHelper;
using Identity.Application.Models.Auth;
using Identity.Application.Validators.Auth;
using Identity.Domain.Enums;

namespace Identity.Application.Tests.Validators;

public class VerifyOtpModelValidatorTests
{
    private readonly VerifyOtpModelValidator _sut = new();

    private static VerifyOtpModel Valid() => new()
    {
        Identifier = "user@test.com",
        Type = IdentifierType.Email,
        Otp = "123456"
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Identifier_Empty_HasError()
        => _sut.TestValidate(Valid() with { Identifier = "" }).ShouldHaveValidationErrorFor(x => x.Identifier);

    [Fact]
    public void Otp_Empty_HasError()
        => _sut.TestValidate(Valid() with { Otp = "" }).ShouldHaveValidationErrorFor(x => x.Otp);

    [Theory]
    [InlineData("12345")]   // too short
    [InlineData("1234567")] // too long
    [InlineData("12a456")]  // non-digit
    public void Otp_InvalidFormat_HasError(string otp)
        => _sut.TestValidate(Valid() with { Otp = otp }).ShouldHaveValidationErrorFor(x => x.Otp);
}
