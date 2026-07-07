using Dojo.Application.Models.IncomeInvoice;
using Dojo.Application.Validators.IncomeInvoices;
using FluentValidation.TestHelper;

namespace Dojo.Application.Tests.Validators;

public class RefundIncomeTransactionModelValidatorTests
{
    private readonly RefundIncomeTransactionModelValidator _sut = new();

    private static RefundIncomeTransactionModel Valid() => new()
    {
        InvoiceId = Guid.NewGuid(),
        TransactionId = Guid.NewGuid(),
        Amount = 25m,
        Reason = "Customer requested refund"
    };

    [Fact]
    public void Valid_Model_HasNoErrors()
        => _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void InvoiceId_Empty_HasError()
        => _sut.TestValidate(Valid() with { InvoiceId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.InvoiceId);

    [Fact]
    public void TransactionId_Empty_HasError()
        => _sut.TestValidate(Valid() with { TransactionId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.TransactionId);

    [Fact]
    public void Amount_Zero_HasError()
        => _sut.TestValidate(Valid() with { Amount = 0 }).ShouldHaveValidationErrorFor(x => x.Amount);

    [Fact]
    public void Reason_Empty_HasError()
        => _sut.TestValidate(Valid() with { Reason = "" }).ShouldHaveValidationErrorFor(x => x.Reason);

    [Fact]
    public void Reason_TooLong_HasError()
        => _sut.TestValidate(Valid() with { Reason = new string('a', 501) }).ShouldHaveValidationErrorFor(x => x.Reason);

    [Fact]
    public void Reason_AtMaxLength_IsAllowed()
        => _sut.TestValidate(Valid() with { Reason = new string('a', 500) }).ShouldNotHaveValidationErrorFor(x => x.Reason);
}
