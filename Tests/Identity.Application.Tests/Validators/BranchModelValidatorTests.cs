using FluentValidation.TestHelper;
using Identity.Application.Models.Branch;
using Identity.Application.Validators.Branch;

namespace Identity.Application.Tests.Validators;

public class BranchModelValidatorTests
{
    private readonly BranchModelValidator _sut = new();

    private static BranchModel Valid() => new()
    {
        TenantId = Guid.NewGuid(),
        Name = "Downtown",
        Email = "branch@test.com"
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_Empty_HasError()
        => _sut.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Email_Empty_HasError()
        => _sut.TestValidate(Valid() with { Email = "" }).ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Email_Invalid_HasError()
        => _sut.TestValidate(Valid() with { Email = "not-an-email" }).ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void PhoneNumber_TooLong_WhenProvided_HasError()
        => _sut.TestValidate(Valid() with { PhoneNumber = new string('1', 51) }).ShouldHaveValidationErrorFor(x => x.PhoneNumber);

    [Fact]
    public void PhoneNumber_Null_IsIgnored()
        => _sut.TestValidate(Valid() with { PhoneNumber = null }).ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);

    [Fact]
    public void AddressStreet_TooLong_WhenProvided_HasError()
        => _sut.TestValidate(Valid() with { AddressStreet = new string('a', 201) }).ShouldHaveValidationErrorFor(x => x.AddressStreet);
}
