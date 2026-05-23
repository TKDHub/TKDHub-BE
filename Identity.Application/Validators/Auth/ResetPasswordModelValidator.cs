using FluentValidation;
using Identity.Application.Models.Auth;
using Identity.Application.Validators.ValidationMessages;

namespace Identity.Application.Validators.Auth;

public sealed class ResetPasswordModelValidator : AbstractValidator<ResetPasswordModel>
{
    public ResetPasswordModelValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage(AuthValidationMessages.IdentifierRequired)
            .MaximumLength(200).WithMessage(AuthValidationMessages.IdentifierMaxLength);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage(AuthValidationMessages.NewPasswordRequired)
            .MinimumLength(8).WithMessage(AuthValidationMessages.PasswordMinLength)
            .Matches(@"[A-Z]").WithMessage(AuthValidationMessages.PasswordUppercase)
            .Matches(@"[a-z]").WithMessage(AuthValidationMessages.PasswordLowercase)
            .Matches(@"[0-9]").WithMessage(AuthValidationMessages.PasswordDigit)
            .Matches(@"[^a-zA-Z0-9]").WithMessage(AuthValidationMessages.PasswordSpecialChar);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage(AuthValidationMessages.ConfirmPasswordRequired)
            .Equal(x => x.NewPassword).WithMessage(AuthValidationMessages.PasswordsMustMatch);
    }
}
