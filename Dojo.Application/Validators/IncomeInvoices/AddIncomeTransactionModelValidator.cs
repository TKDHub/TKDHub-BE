using Dojo.Application.Models.IncomeInvoice;
using Dojo.Application.Validators.ValidationMessages;
using FluentValidation;

namespace Dojo.Application.Validators.IncomeInvoices;

public sealed class AddIncomeTransactionModelValidator : AbstractValidator<AddIncomeTransactionModel>
{
    public AddIncomeTransactionModelValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage(IncomeInvoiceValidationMessages.AmountInvalid);

        RuleFor(x => x.Method)
            .IsInEnum().WithMessage(IncomeInvoiceValidationMessages.MethodInvalid);
    }
}
