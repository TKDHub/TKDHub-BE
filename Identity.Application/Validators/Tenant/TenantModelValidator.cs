using FluentValidation;
using Identity.Application.Models.Tenant;

namespace Identity.Application.Validators.Tenant
{
    public sealed class TenantModelValidator : AbstractValidator<TenantModel>
    {
        public TenantModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tenant name is required.")
                .MaximumLength(200).WithMessage("Tenant name must not exceed 200 characters.");

            RuleFor(x => x.Subdomain)
                .NotEmpty().WithMessage("Subdomain is required.")
                .MaximumLength(50).WithMessage("Subdomain must not exceed 50 characters.")
                .Matches("^[a-z0-9-]+$")
                .WithMessage("Subdomain can contain only lowercase letters, numbers, and hyphens.");

            RuleFor(x => x.ContactEmail)
                .NotEmpty().WithMessage("Contact email is required.")
                .EmailAddress().WithMessage("Contact email must be a valid email address.")
                .MaximumLength(256).WithMessage("Contact email must not exceed 256 characters.");
        }
    }
}