using Dojo.Application.Models.Student;
using Dojo.Application.Validators.ValidationMessages;
using FluentValidation;

namespace Dojo.Application.Validators.Students;

public sealed class StudentModelValidator : AbstractValidator<StudentModel>
{
    public StudentModelValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(StudentValidationMessages.FirstNameRequired)
            .MaximumLength(150).WithMessage(StudentValidationMessages.FirstNameMaxLength);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(StudentValidationMessages.LastNameRequired)
            .MaximumLength(150).WithMessage(StudentValidationMessages.LastNameMaxLength);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage(StudentValidationMessages.PhoneRequired)
            .MaximumLength(50).WithMessage(StudentValidationMessages.PhoneMaxLength);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage(StudentValidationMessages.EmailInvalid)
            .MaximumLength(150).WithMessage(StudentValidationMessages.EmailMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.DateOfBirth)
            .Must(d => d < DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage(StudentValidationMessages.DateOfBirthPast)
            .Must(d => d > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-120)))
                .WithMessage(StudentValidationMessages.DateOfBirthInvalid);

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage(StudentValidationMessages.GenderRequired);

        RuleFor(x => x.StartDate)
            .Must(d => d != DateOnly.MinValue)
                .WithMessage(StudentValidationMessages.StartDateRequired);

        RuleFor(x => x.BeltLevel)
            .NotEmpty().WithMessage(StudentValidationMessages.BeltLevelRequired);

        RuleFor(x => x.SubscriptionPlanId)
            .NotEmpty().WithMessage(StudentValidationMessages.PlanRequired);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage(StudentValidationMessages.PriceInvalid);

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage(StudentValidationMessages.CurrencyRequired)
            .MaximumLength(10).WithMessage(StudentValidationMessages.CurrencyMaxLength);

        RuleFor(x => x.DurationMonths)
            .GreaterThanOrEqualTo(1).WithMessage(StudentValidationMessages.DurationInvalid);

        RuleFor(x => x.EmergencyContact)
            .MaximumLength(200).WithMessage(StudentValidationMessages.EmergencyMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.EmergencyContact));

        RuleFor(x => x.ProfileImageUrl)
            .MaximumLength(500).WithMessage(StudentValidationMessages.ImageUrlMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.ProfileImageUrl));
    }
}
