using FluentValidation.TestHelper;
using Identity.Application.Models.Tenant;
using Identity.Application.Validators.Tenant;

namespace Identity.Application.Tests.Validators;

public class TenantModelValidatorTests
{
    private readonly TenantModelValidator _sut = new();

    private static TenantModel Valid() => new()
    {
        Name = "Acme Dojo",
        Subdomain = "acme-dojo",
        ContactEmail = "contact@acme.com"
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_Empty_HasError()
        => _sut.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Subdomain_Empty_HasError()
        => _sut.TestValidate(Valid() with { Subdomain = "" }).ShouldHaveValidationErrorFor(x => x.Subdomain);

    [Theory]
    [InlineData("Has Spaces")]
    [InlineData("Has_Underscore")]
    [InlineData("UPPERCASE")]
    [InlineData("has.dot")]
    public void Subdomain_InvalidFormat_HasError(string subdomain)
        => _sut.TestValidate(Valid() with { Subdomain = subdomain }).ShouldHaveValidationErrorFor(x => x.Subdomain);

    [Fact]
    public void ContactEmail_Invalid_HasError()
        => _sut.TestValidate(Valid() with { ContactEmail = "not-an-email" }).ShouldHaveValidationErrorFor(x => x.ContactEmail);
}
