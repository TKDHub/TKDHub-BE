using FluentValidation.TestHelper;
using Identity.Application.Models.User;
using Identity.Application.Validators.Users;

namespace Identity.Application.Tests.Validators;

public class RegisterUserModelValidatorTests
{
    private readonly RegisterUserModelValidator _sut = new();

    private static RegisterUserModel Valid() => new()
    {
        TenantId = Guid.NewGuid(),
        Username = "newuser",
        Email = "newuser@test.com",
        Password = "NewPass1!",
        ConfirmPassword = "NewPass1!"
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Username_Empty_HasError()
        => _sut.TestValidate(Valid() with { Username = "" }).ShouldHaveValidationErrorFor(x => x.Username);

    [Fact]
    public void Username_TooLong_HasError()
        => _sut.TestValidate(Valid() with { Username = new string('a', 51) }).ShouldHaveValidationErrorFor(x => x.Username);

    [Fact]
    public void Email_Empty_HasError()
        => _sut.TestValidate(Valid() with { Email = "" }).ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Email_Invalid_HasError()
        => _sut.TestValidate(Valid() with { Email = "not-an-email" }).ShouldHaveValidationErrorFor(x => x.Email);

    [Theory]
    [InlineData("short1!")]
    [InlineData("nouppercase1!")]
    [InlineData("NOLOWERCASE1!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NoSpecialChar1")]
    public void Password_FailsComplexityRules(string password)
        => _sut.TestValidate(Valid() with { Password = password, ConfirmPassword = password })
            .ShouldHaveValidationErrorFor(x => x.Password);

    [Fact]
    public void ConfirmPassword_NotMatching_HasError()
        => _sut.TestValidate(Valid() with { ConfirmPassword = "Different1!" })
            .ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
}
