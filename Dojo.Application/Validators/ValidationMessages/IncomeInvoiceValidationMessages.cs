namespace Dojo.Application.Validators.ValidationMessages;

public static class IncomeInvoiceValidationMessages
{
    public const string StudentRequired      = "Student is required.";
    public const string TypeInvalid          = "Income invoice type is invalid.";
    public const string OriginalPriceInvalid = "Original price must be greater than zero.";
    public const string DiscountValueInvalid = "Discount value must be zero or greater.";
    public const string DiscountPercentRange = "A percentage discount must be between 0 and 100.";
    public const string PaymentMethodInvalid = "Payment method is invalid.";
    public const string AmountInvalid        = "Amount must be greater than zero.";
    public const string MethodInvalid        = "Payment method is invalid.";
}
