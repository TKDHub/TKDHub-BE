using FluentValidation.TestHelper;
using Identity.Application.Models.Auth;
using Identity.Application.Validators.Users;

namespace Identity.Application.Tests.Validators;

public class AuthModelValidatorTests
{
    private readonly AuthModelValidator _sut = new();

    private static AuthModel Valid() => new()
    {
        Username = "user@test.com",
        Password = "whatever-they-typed"
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Username_Empty_HasError()
        => _sut.TestValidate(Valid() with { Username = "" }).ShouldHaveValidationErrorFor(x => x.Username);

    [Fact]
    public void Username_TooLong_HasError()
        => _sut.TestValidate(Valid() with { Username = new string('a', 151) }).ShouldHaveValidationErrorFor(x => x.Username);

    [Fact]
    public void Password_Empty_HasError()
        => _sut.TestValidate(Valid() with { Password = "" }).ShouldHaveValidationErrorFor(x => x.Password);
}
