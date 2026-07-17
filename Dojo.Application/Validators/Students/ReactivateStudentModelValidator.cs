using Dojo.Application.Models.Student;
using Dojo.Application.Validators.ValidationMessages;
using FluentValidation;

namespace Dojo.Application.Validators.Students;

public sealed class ReactivateStudentModelValidator : AbstractValidator<ReactivateStudentModel>
{
    public ReactivateStudentModelValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage(StudentValidationMessages.StudentIdRequired);

        RuleFor(x => x.StartDate)
            .Must(d => d != DateOnly.MinValue)
                .WithMessage(StudentValidationMessages.StartDateRequired);
    }
}
