using FluentValidation;
using Identity.Application.Models.Auth;
using Identity.Application.Validators.ValidationMessages;

namespace Identity.Application.Validators.Auth;

public sealed class ForgotPasswordModelValidator : AbstractValidator<ForgotPasswordModel>
{
    public ForgotPasswordModelValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage(AuthValidationMessages.IdentifierRequired)
            .MaximumLength(200).WithMessage(AuthValidationMessages.IdentifierMaxLength);
    }
}
