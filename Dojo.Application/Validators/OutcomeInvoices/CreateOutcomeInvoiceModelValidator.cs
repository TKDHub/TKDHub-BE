using Dojo.Application.Models.OutcomeInvoice;
using Dojo.Application.Validators.ValidationMessages;
using FluentValidation;

namespace Dojo.Application.Validators.OutcomeInvoices;

public sealed class CreateOutcomeInvoiceModelValidator : AbstractValidator<CreateOutcomeInvoiceModel>
{
    public CreateOutcomeInvoiceModelValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(OutcomeInvoiceValidationMessages.TitleRequired)
            .MaximumLength(200).WithMessage(OutcomeInvoiceValidationMessages.TitleMaxLength);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage(OutcomeInvoiceValidationMessages.AmountInvalid);

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage(OutcomeInvoiceValidationMessages.NoteMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
