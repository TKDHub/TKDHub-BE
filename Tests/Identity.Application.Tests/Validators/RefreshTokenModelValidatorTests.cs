using FluentValidation.TestHelper;
using Identity.Application.Models.Auth;
using Identity.Application.Validators.Auth;

namespace Identity.Application.Tests.Validators;

public class RefreshTokenModelValidatorTests
{
    private readonly RefreshTokenModelValidator _sut = new();

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(new RefreshTokenModel { RefreshToken = "a-real-token" }).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void RefreshToken_Empty_HasError()
        => _sut.TestValidate(new RefreshTokenModel { RefreshToken = "" }).ShouldHaveValidationErrorFor(x => x.RefreshToken);
}
