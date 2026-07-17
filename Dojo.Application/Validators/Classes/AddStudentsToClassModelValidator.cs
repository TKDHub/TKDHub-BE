using Dojo.Application.Models.Class;
using Dojo.Application.Validators.ValidationMessages;
using FluentValidation;

namespace Dojo.Application.Validators.Classes;

public sealed class AddStudentsToClassModelValidator : AbstractValidator<AddStudentsToClassModel>
{
    public AddStudentsToClassModelValidator()
    {
        RuleFor(x => x.ClassId)
            .NotEmpty().WithMessage(ClassValidationMessages.ClassIdRequired);

        RuleFor(x => x.StudentIds)
            .NotEmpty().WithMessage(ClassValidationMessages.StudentIdsRequired);
    }
}
