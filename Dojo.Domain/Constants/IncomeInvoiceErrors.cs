using Shared.Domain.Primitives;

namespace Dojo.Domain.Constants;

public static class IncomeInvoiceErrors
{
    public static readonly Error NotFound          = new("IncomeInvoice.NotFound",          "Income invoice not found.");
    public static readonly Error StudentRequired   = new("IncomeInvoice.StudentRequired",   "Student ID is required.");
    public static readonly Error StudentNotFound   = new("IncomeInvoice.StudentNotFound",   "Student not found.");
    public static readonly Error PriceInvalid      = new("IncomeInvoice.PriceInvalid",      "Original price must be greater than zero.");
    public static readonly Error DiscountInvalid   = new("IncomeInvoice.DiscountInvalid",   "Discount value is invalid for the selected discount type.");
    public static readonly Error PaymentMethodRequired = new("IncomeInvoice.PaymentMethodRequired", "A payment method is required when recording a payment.");
    public static readonly Error PaymentExceedsTotal   = new("IncomeInvoice.PaymentExceedsTotal",   "Amount paid cannot exceed the amount due.");
    public static readonly Error AlreadyClosed     = new("IncomeInvoice.AlreadyClosed",     "Income invoice is already fully paid and closed.");
    public static readonly Error TransactionAmountInvalid = new("IncomeInvoice.TransactionAmountInvalid", "Transaction amount must be greater than zero and cannot exceed the remaining balance.");
}
