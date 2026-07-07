using Dojo.Application.Models.IncomeInvoice;
using Dojo.Application.Validators.IncomeInvoices;
using FluentValidation.TestHelper;

namespace Dojo.Application.Tests.Validators;

public class VoidIncomeInvoiceModelValidatorTests
{
    private readonly VoidIncomeInvoiceModelValidator _sut = new();

    private static VoidIncomeInvoiceModel Valid() => new()
    {
        InvoiceId = Guid.NewGuid(),
        Reason = "Duplicate invoice"
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Reason_Empty_HasError()
        => _sut.TestValidate(Valid() with { Reason = "" }).ShouldHaveValidationErrorFor(x => x.Reason);

    [Fact]
    public void Reason_TooLong_HasError()
        => _sut.TestValidate(Valid() with { Reason = new string('a', 501) }).ShouldHaveValidationErrorFor(x => x.Reason);
}
