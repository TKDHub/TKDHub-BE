using FluentValidation;
using Identity.Application.Models.Auth;
using Identity.Application.Validators.ValidationMessages;

namespace Identity.Application.Validators.Auth;

public sealed class RefreshTokenModelValidator : AbstractValidator<RefreshTokenModel>
{
    public RefreshTokenModelValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage(AuthValidationMessages.RefreshTokenRequired);
    }
}
