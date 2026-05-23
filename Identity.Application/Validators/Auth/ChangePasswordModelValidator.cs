using FluentValidation;
using Identity.Application.Models.Auth;
using Identity.Application.Validators.ValidationMessages;

namespace Identity.Application.Validators.Auth;

public sealed class ChangePasswordModelValidator : AbstractValidator<ChangePasswordModel>
{
    public ChangePasswordModelValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage(AuthValidationMessages.OldPasswordRequired);

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
