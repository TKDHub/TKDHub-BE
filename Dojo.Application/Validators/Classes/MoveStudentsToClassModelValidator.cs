using Dojo.Application.Models.Class;
using Dojo.Application.Validators.ValidationMessages;
using FluentValidation;

namespace Dojo.Application.Validators.Classes;

public sealed class MoveStudentsToClassModelValidator : AbstractValidator<MoveStudentsToClassModel>
{
    public MoveStudentsToClassModelValidator()
    {
        RuleFor(x => x.FromClassId)
            .NotEmpty().WithMessage(ClassValidationMessages.FromClassIdRequired);

        RuleFor(x => x.ToClassId)
            .NotEmpty().WithMessage(ClassValidationMessages.ToClassIdRequired);

        RuleFor(x => x.StudentIds)
            .NotEmpty().WithMessage(ClassValidationMessages.StudentIdsRequired);

        RuleFor(x => x)
            .Must(x => x.FromClassId != x.ToClassId)
            .WithMessage(ClassValidationMessages.FromToClassMustDiffer)
            .WithName(nameof(MoveStudentsToClassModel.ToClassId));
    }
}
