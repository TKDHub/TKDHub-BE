using Dojo.Application.Models.IncomeInvoice;
using Dojo.Application.Validators.ValidationMessages;
using Dojo.Domain.Enums;
using FluentValidation;

namespace Dojo.Application.Validators.IncomeInvoices;

public sealed class CreateIncomeInvoiceModelValidator : AbstractValidator<CreateIncomeInvoiceModel>
{
    public CreateIncomeInvoiceModelValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage(IncomeInvoiceValidationMessages.StudentRequired);

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage(IncomeInvoiceValidationMessages.TypeInvalid);

        RuleFor(x => x.OriginalPrice)
            .GreaterThan(0).WithMessage(IncomeInvoiceValidationMessages.OriginalPriceInvalid);

        // Discount: when a type is chosen the value must be valid for that type.
        RuleFor(x => x.DiscountValue)
            .GreaterThanOrEqualTo(0).WithMessage(IncomeInvoiceValidationMessages.DiscountValueInvalid)
            .When(x => x.DiscountType is not null);

        RuleFor(x => x.DiscountValue)
            .InclusiveBetween(0, 100).WithMessage(IncomeInvoiceValidationMessages.DiscountPercentRange)
            .When(x => x.DiscountType == DiscountTypeEnum.Percentage);

        // An amount collected now, if provided, must be positive.
        RuleFor(x => x.AmountPaid)
            .GreaterThan(0).WithMessage(IncomeInvoiceValidationMessages.AmountInvalid)
            .When(x => x.AmountPaid is not null);

        // A payment method is only required when money is actually collected.
        RuleFor(x => x.PaymentMethod)
            .NotNull().IsInEnum().WithMessage(IncomeInvoiceValidationMessages.PaymentMethodInvalid)
            .When(x => x.AmountPaid is > 0);
    }
}
