using FluentValidation.TestHelper;
using Identity.Application.Models.Auth;
using Identity.Application.Validators.Auth;

namespace Identity.Application.Tests.Validators;

public class ResetPasswordModelValidatorTests
{
    private readonly ResetPasswordModelValidator _sut = new();

    private static ResetPasswordModel Valid() => new()
    {
        Identifier = "user@test.com",
        NewPassword = "NewPass1!",
        ConfirmPassword = "NewPass1!"
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Identifier_Empty_HasError()
        => _sut.TestValidate(Valid() with { Identifier = "" }).ShouldHaveValidationErrorFor(x => x.Identifier);

    [Fact]
    public void NewPassword_TooShort_HasError()
        => _sut.TestValidate(Valid() with { NewPassword = "Sh0rt!", ConfirmPassword = "Sh0rt!" })
            .ShouldHaveValidationErrorFor(x => x.NewPassword);

    [Fact]
    public void ConfirmPassword_NotMatching_HasError()
        => _sut.TestValidate(Valid() with { ConfirmPassword = "Different1!" })
            .ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
}
