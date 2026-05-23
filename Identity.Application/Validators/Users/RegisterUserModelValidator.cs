using FluentValidation;
using Identity.Application.Models.User;
using Identity.Application.Validators.ValidationMessages;

namespace Identity.Application.Validators.Users;

public sealed class RegisterUserModelValidator : AbstractValidator<RegisterUserModel>
{
    public RegisterUserModelValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage(UserValidationMessages.UsernameRequired)
            .MaximumLength(50).WithMessage(UserValidationMessages.UsernameMaxLength);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(UserValidationMessages.EmailRequired)
            .EmailAddress().WithMessage(UserValidationMessages.EmailInvalid)
            .MaximumLength(100).WithMessage(UserValidationMessages.EmailMaxLength100);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(AuthValidationMessages.PasswordRequired)
            .MinimumLength(8).WithMessage(AuthValidationMessages.PasswordMinLength)
            .Matches(@"[A-Z]").WithMessage(AuthValidationMessages.PasswordUppercase)
            .Matches(@"[a-z]").WithMessage(AuthValidationMessages.PasswordLowercase)
            .Matches(@"[0-9]").WithMessage(AuthValidationMessages.PasswordDigit)
            .Matches(@"[^a-zA-Z0-9]").WithMessage(AuthValidationMessages.PasswordSpecialChar);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage(AuthValidationMessages.ConfirmPasswordRequired)
            .Equal(x => x.Password).WithMessage(AuthValidationMessages.PasswordsMustMatch);
    }
}
