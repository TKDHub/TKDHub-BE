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

    public const string VoidReasonRequired   = "A reason is required to void an invoice.";
    public const string ReasonMaxLength      = "Reason cannot exceed 500 characters.";
    public const string RefundReasonRequired = "A reason is required to refund a transaction.";
    public const string InvoiceIdRequired    = "Invoice ID is required.";
    public const string TransactionIdRequired = "Transaction ID is required.";
}
