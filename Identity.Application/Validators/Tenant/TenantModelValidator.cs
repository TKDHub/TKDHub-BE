using FluentValidation;
using Identity.Application.Models.Tenant;
using Identity.Application.Validators.ValidationMessages;

namespace Identity.Application.Validators.Tenant;

public sealed class TenantModelValidator : AbstractValidator<TenantModel>
{
    public TenantModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(TenantValidationMessages.NameRequired)
            .MaximumLength(200).WithMessage(TenantValidationMessages.NameMaxLength);

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage(TenantValidationMessages.SubdomainRequired)
            .MaximumLength(50).WithMessage(TenantValidationMessages.SubdomainMaxLength)
            .Matches("^[a-z0-9-]+$").WithMessage(TenantValidationMessages.SubdomainFormat);

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage(TenantValidationMessages.ContactEmailRequired)
            .EmailAddress().WithMessage(TenantValidationMessages.ContactEmailInvalid)
            .MaximumLength(256).WithMessage(TenantValidationMessages.ContactEmailMaxLength);
    }
}
