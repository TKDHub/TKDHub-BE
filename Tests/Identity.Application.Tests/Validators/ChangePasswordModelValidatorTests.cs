using FluentValidation.TestHelper;
using Identity.Application.Models.Auth;
using Identity.Application.Validators.Auth;

namespace Identity.Application.Tests.Validators;

public class ChangePasswordModelValidatorTests
{
    private readonly ChangePasswordModelValidator _sut = new();

    private static ChangePasswordModel Valid() => new()
    {
        OldPassword = "OldPass1!",
        NewPassword = "NewPass1!",
        ConfirmPassword = "NewPass1!"
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void OldPassword_Empty_HasError()
        => _sut.TestValidate(Valid() with { OldPassword = "" }).ShouldHaveValidationErrorFor(x => x.OldPassword);

    [Theory]
    [InlineData("short1!")]      // too short
    [InlineData("nouppercase1!")] // no uppercase
    [InlineData("NOLOWERCASE1!")] // no lowercase
    [InlineData("NoDigitsHere!")] // no digit
    [InlineData("NoSpecialChar1")] // no special char
    public void NewPassword_FailsComplexityRules(string password)
        => _sut.TestValidate(Valid() with { NewPassword = password, ConfirmPassword = password })
            .ShouldHaveValidationErrorFor(x => x.NewPassword);

    [Fact]
    public void ConfirmPassword_NotMatchingNewPassword_HasError()
        => _sut.TestValidate(Valid() with { ConfirmPassword = "Different1!" })
            .ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
}
