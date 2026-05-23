using FluentValidation;
using Identity.Application.Models.Auth;
using Identity.Application.Validators.ValidationMessages;

namespace Identity.Application.Validators.Auth;

public sealed class VerifyOtpModelValidator : AbstractValidator<VerifyOtpModel>
{
    public VerifyOtpModelValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage(AuthValidationMessages.IdentifierRequired)
            .MaximumLength(200).WithMessage(AuthValidationMessages.IdentifierMaxLength);

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage(AuthValidationMessages.OtpRequired)
            .Length(6).WithMessage(AuthValidationMessages.OtpLength)
            .Matches(@"^\d{6}$").WithMessage(AuthValidationMessages.OtpDigitsOnly);
    }
}
