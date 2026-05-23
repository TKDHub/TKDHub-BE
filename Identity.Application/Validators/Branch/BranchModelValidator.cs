using FluentValidation;
using Identity.Application.Models.Branch;
using Identity.Application.Validators.ValidationMessages;

namespace Identity.Application.Validators.Branch;

public sealed class BranchModelValidator : AbstractValidator<BranchModel>
{
    public BranchModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(BranchValidationMessages.NameRequired)
            .MaximumLength(200).WithMessage(BranchValidationMessages.NameMaxLength);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(BranchValidationMessages.EmailRequired)
            .EmailAddress().WithMessage(BranchValidationMessages.EmailInvalid)
            .MaximumLength(200).WithMessage(BranchValidationMessages.EmailMaxLength);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50).WithMessage(BranchValidationMessages.PhoneMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Currency)
            .MaximumLength(10).WithMessage(BranchValidationMessages.CurrencyMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));

        RuleFor(x => x.AddressCountry)
            .MaximumLength(100).WithMessage(BranchValidationMessages.CountryMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.AddressCountry));

        RuleFor(x => x.AddressState)
            .MaximumLength(100).WithMessage(BranchValidationMessages.StateMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.AddressState));

        RuleFor(x => x.AddressCity)
            .MaximumLength(100).WithMessage(BranchValidationMessages.CityMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.AddressCity));

        RuleFor(x => x.AddressStreet)
            .MaximumLength(200).WithMessage(BranchValidationMessages.StreetMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.AddressStreet));
    }
}
