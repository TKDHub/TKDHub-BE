using Dojo.Application.Models.IncomeInvoice;
using Dojo.Application.Validators.IncomeInvoices;
using Dojo.Domain.Enums;
using FluentValidation.TestHelper;

namespace Dojo.Application.Tests.Validators;

public class CreateIncomeInvoiceModelValidatorTests
{
    private readonly CreateIncomeInvoiceModelValidator _sut = new();

    private static CreateIncomeInvoiceModel Valid() => new()
    {
        StudentId = Guid.NewGuid(),
        Type = IncomeInvoiceTypeEnum.Subscription,
        OriginalPrice = 100m
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void StudentId_Empty_HasError()
        => _sut.TestValidate(Valid() with { StudentId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.StudentId);

    [Fact]
    public void Type_InvalidEnum_HasError()
        => _sut.TestValidate(Valid() with { Type = (IncomeInvoiceTypeEnum)999 }).ShouldHaveValidationErrorFor(x => x.Type);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void OriginalPrice_MustBeGreaterThanZero(decimal price)
        => _sut.TestValidate(Valid() with { OriginalPrice = price }).ShouldHaveValidationErrorFor(x => x.OriginalPrice);

    [Fact]
    public void DiscountValue_Negative_WhenDiscountTypeSet_HasError()
        => _sut.TestValidate(Valid() with { DiscountType = DiscountTypeEnum.Value, DiscountValue = -1 })
            .ShouldHaveValidationErrorFor(x => x.DiscountValue);

    [Fact]
    public void DiscountValue_Negative_WhenDiscountTypeNull_IsIgnored()
        => _sut.TestValidate(Valid() with { DiscountType = null, DiscountValue = -1 })
            .ShouldNotHaveValidationErrorFor(x => x.DiscountValue);

    [Theory]
    [InlineData(101)]
    [InlineData(-1)]
    public void DiscountValue_OutOfPercentRange_WhenPercentageType_HasError(decimal value)
        => _sut.TestValidate(Valid() with { DiscountType = DiscountTypeEnum.Percentage, DiscountValue = value })
            .ShouldHaveValidationErrorFor(x => x.DiscountValue);

    [Fact]
    public void DiscountValue_150_WhenFlatValueType_IsAllowed()
        => _sut.TestValidate(Valid() with { DiscountType = DiscountTypeEnum.Value, DiscountValue = 150 })
            .ShouldNotHaveValidationErrorFor(x => x.DiscountValue);

    [Fact]
    public void AmountPaid_Zero_WhenProvided_HasError()
        => _sut.TestValidate(Valid() with { AmountPaid = 0 }).ShouldHaveValidationErrorFor(x => x.AmountPaid);

    [Fact]
    public void AmountPaid_Null_IsIgnored()
        => _sut.TestValidate(Valid() with { AmountPaid = null }).ShouldNotHaveValidationErrorFor(x => x.AmountPaid);

    [Fact]
    public void PaymentMethod_Null_WhenAmountPaidPositive_HasError()
        => _sut.TestValidate(Valid() with { AmountPaid = 50, PaymentMethod = null })
            .ShouldHaveValidationErrorFor(x => x.PaymentMethod);

    [Fact]
    public void PaymentMethod_Null_WhenNothingPaid_IsIgnored()
        => _sut.TestValidate(Valid() with { AmountPaid = null, PaymentMethod = null })
            .ShouldNotHaveValidationErrorFor(x => x.PaymentMethod);
}
