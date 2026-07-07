using FluentValidation.TestHelper;
using Identity.Application.Models.User;
using Identity.Application.Validators.Users;

namespace Identity.Application.Tests.Validators;

public class UpdateAccountModelValidatorTests
{
    private readonly UpdateAccountModelValidator _sut = new();

    private static UpdateAccountModel Valid() => new()
    {
        UserId = Guid.NewGuid(),
        Email = "user@test.com",
        PhoneNumber = "0700000000"
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Email_Invalid_WhenProvided_HasError()
        => _sut.TestValidate(Valid() with { Email = "not-an-email" }).ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Email_Null_IsIgnored()
        => _sut.TestValidate(Valid() with { Email = null }).ShouldNotHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void PhoneNumber_TooLong_WhenProvided_HasError()
        => _sut.TestValidate(Valid() with { PhoneNumber = new string('1', 51) }).ShouldHaveValidationErrorFor(x => x.PhoneNumber);

    [Fact]
    public void PhoneNumber_Null_IsIgnored()
        => _sut.TestValidate(Valid() with { PhoneNumber = null }).ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
}
