using Dojo.Application.Models.IncomeInvoice;
using Dojo.Application.Validators.ValidationMessages;
using FluentValidation;

namespace Dojo.Application.Validators.IncomeInvoices;

public sealed class RefundIncomeTransactionModelValidator : AbstractValidator<RefundIncomeTransactionModel>
{
    public RefundIncomeTransactionModelValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty().WithMessage(IncomeInvoiceValidationMessages.InvoiceIdRequired);

        RuleFor(x => x.TransactionId)
            .NotEmpty().WithMessage(IncomeInvoiceValidationMessages.TransactionIdRequired);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage(IncomeInvoiceValidationMessages.AmountInvalid);

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage(IncomeInvoiceValidationMessages.RefundReasonRequired)
            .MaximumLength(500).WithMessage(IncomeInvoiceValidationMessages.ReasonMaxLength);
    }
}
