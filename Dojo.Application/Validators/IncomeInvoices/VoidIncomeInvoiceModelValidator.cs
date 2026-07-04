using Dojo.Application.Models.IncomeInvoice;
using Dojo.Application.Validators.ValidationMessages;
using FluentValidation;

namespace Dojo.Application.Validators.IncomeInvoices;

public sealed class VoidIncomeInvoiceModelValidator : AbstractValidator<VoidIncomeInvoiceModel>
{
    public VoidIncomeInvoiceModelValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage(IncomeInvoiceValidationMessages.VoidReasonRequired)
            .MaximumLength(500).WithMessage(IncomeInvoiceValidationMessages.ReasonMaxLength);
    }
}
