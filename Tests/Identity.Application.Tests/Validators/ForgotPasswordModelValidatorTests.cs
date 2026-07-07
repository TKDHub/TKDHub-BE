using FluentValidation.TestHelper;
using Identity.Application.Models.Auth;
using Identity.Application.Validators.Auth;
using Identity.Domain.Enums;

namespace Identity.Application.Tests.Validators;

public class ForgotPasswordModelValidatorTests
{
    private readonly ForgotPasswordModelValidator _sut = new();

    private static ForgotPasswordModel Valid() => new()
    {
        Identifier = "user@test.com",
        Type = IdentifierType.Email
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Identifier_Empty_HasError()
        => _sut.TestValidate(Valid() with { Identifier = "" }).ShouldHaveValidationErrorFor(x => x.Identifier);

    [Fact]
    public void Identifier_TooLong_HasError()
        => _sut.TestValidate(Valid() with { Identifier = new string('a', 201) }).ShouldHaveValidationErrorFor(x => x.Identifier);
}
