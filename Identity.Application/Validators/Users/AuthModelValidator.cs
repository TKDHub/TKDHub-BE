using FluentValidation;
using Identity.Application.Models.Auth;
using Identity.Application.Validators.ValidationMessages;

namespace Identity.Application.Validators.Users;

public sealed class AuthModelValidator : AbstractValidator<AuthModel>
{
    public AuthModelValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage(AuthValidationMessages.UsernameRequired)
            .MaximumLength(150).WithMessage(AuthValidationMessages.UsernameMaxLength);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(AuthValidationMessages.PasswordRequired);
    }
}
