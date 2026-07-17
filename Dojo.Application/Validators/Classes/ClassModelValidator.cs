using Dojo.Application.Models.Class;
using Dojo.Application.Validators.ValidationMessages;
using FluentValidation;

namespace Dojo.Application.Validators.Classes;

public sealed class ClassModelValidator : AbstractValidator<ClassModel>
{
    public ClassModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(ClassValidationMessages.NameRequired)
            .MaximumLength(200).WithMessage(ClassValidationMessages.NameMaxLength);

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime).WithMessage(ClassValidationMessages.EndTimeAfterStartTime);

        RuleFor(x => x.Weekdays)
            .NotEmpty().WithMessage(ClassValidationMessages.WeekdaysRequired);

        RuleForEach(x => x.Weekdays)
            .IsInEnum().WithMessage(ClassValidationMessages.WeekdayInvalid);
    }
}
