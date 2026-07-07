using FluentValidation.TestHelper;
using Identity.Application.Models.User;
using Identity.Application.Validators.Users;

namespace Identity.Application.Tests.Validators;

public class UpdateUserModelValidatorTests
{
    private readonly UpdateUserModelValidator _sut = new();

    private static UpdateUserModel Valid() => new()
    {
        UserId = Guid.NewGuid(),
        Username = "updateduser",
        PhoneNumber = "0700000000"
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
    public void PhoneNumber_TooLong_WhenProvided_HasError()
        => _sut.TestValidate(Valid() with { PhoneNumber = new string('1', 51) }).ShouldHaveValidationErrorFor(x => x.PhoneNumber);

    [Fact]
    public void PhoneNumber_Null_IsIgnored()
        => _sut.TestValidate(Valid() with { PhoneNumber = null }).ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
}
