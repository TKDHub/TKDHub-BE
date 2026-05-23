using FluentValidation;
using Identity.Application.Models.User;
using Identity.Application.Validators.ValidationMessages;

namespace Identity.Application.Validators.Users;

public sealed class UpdateUserModelValidator : AbstractValidator<UpdateUserModel>
{
    public UpdateUserModelValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage(UserValidationMessages.UsernameRequired)
            .MaximumLength(150).WithMessage(UserValidationMessages.UsernameMaxLength);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50).WithMessage(UserValidationMessages.PhoneMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
