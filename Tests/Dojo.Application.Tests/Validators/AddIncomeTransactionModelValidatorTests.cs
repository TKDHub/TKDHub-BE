using Dojo.Application.Models.IncomeInvoice;
using Dojo.Application.Validators.IncomeInvoices;
using Dojo.Domain.Enums;
using FluentValidation.TestHelper;

namespace Dojo.Application.Tests.Validators;

public class AddIncomeTransactionModelValidatorTests
{
    private readonly AddIncomeTransactionModelValidator _sut = new();

    private static AddIncomeTransactionModel Valid() => new()
    {
        IncomeInvoiceId = Guid.NewGuid(),
        Amount = 50m,
        Method = PaymentMethodEnum.Cash
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Amount_MustBeGreaterThanZero(decimal amount)
        => _sut.TestValidate(Valid() with { Amount = amount }).ShouldHaveValidationErrorFor(x => x.Amount);

    [Fact]
    public void Method_MustBeValidEnumValue()
        => _sut.TestValidate(Valid() with { Method = (PaymentMethodEnum)999 }).ShouldHaveValidationErrorFor(x => x.Method);
}
