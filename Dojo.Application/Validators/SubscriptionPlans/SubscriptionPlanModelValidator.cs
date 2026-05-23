using Dojo.Application.Models.SubscriptionPlan;
using Dojo.Application.Validators.ValidationMessages;
using FluentValidation;

namespace Dojo.Application.Validators.SubscriptionPlans;

public sealed class SubscriptionPlanModelValidator : AbstractValidator<SubscriptionPlanModel>
{
    public SubscriptionPlanModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(SubscriptionPlanValidationMessages.NameRequired)
            .MaximumLength(200).WithMessage(SubscriptionPlanValidationMessages.NameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage(SubscriptionPlanValidationMessages.DescriptionMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.DurationMonths)
            .GreaterThanOrEqualTo(1).WithMessage(SubscriptionPlanValidationMessages.DurationInvalid);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage(SubscriptionPlanValidationMessages.PriceInvalid);
    }
}
