using FluentValidation;
using Identity.Application.Models.Auth;

namespace Identity.Application.Validators.Users
{
    public sealed class AuthModelValidator : AbstractValidator<AuthModel>
    {
        public AuthModelValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MaximumLength(150).WithMessage("Username must not exceed 150 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }
}
