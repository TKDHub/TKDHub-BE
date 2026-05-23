using FluentValidation;
using Identity.Application.Models.User;
using Identity.Application.Validators.ValidationMessages;

namespace Identity.Application.Validators.Users;

public sealed class UpdateAccountModelValidator : AbstractValidator<UpdateAccountModel>
{
    public UpdateAccountModelValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage(UserValidationMessages.EmailInvalid)
            .MaximumLength(150).WithMessage(UserValidationMessages.EmailMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50).WithMessage(UserValidationMessages.PhoneMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
