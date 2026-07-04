using Shared.Domain.Primitives;

namespace Dojo.Domain.Constants;

public static class IncomeInvoiceErrors
{
    public static readonly Error NotFound          = new("IncomeInvoice.NotFound",          "Income invoice not found.");
    public static readonly Error StudentRequired   = new("IncomeInvoice.StudentRequired",   "Student ID is required.");
    public static readonly Error StudentNotFound   = new("IncomeInvoice.StudentNotFound",   "Student not found.");
    public static readonly Error BranchNotFound    = new("IncomeInvoice.BranchNotFound",    "Branch not found.");
    public static readonly Error PriceInvalid      = new("IncomeInvoice.PriceInvalid",      "Original price must be greater than zero.");
    public static readonly Error DiscountInvalid   = new("IncomeInvoice.DiscountInvalid",   "Discount value is invalid for the selected discount type.");
    public static readonly Error PaymentMethodRequired = new("IncomeInvoice.PaymentMethodRequired", "A payment method is required when recording a payment.");
    public static readonly Error PaymentExceedsTotal   = new("IncomeInvoice.PaymentExceedsTotal",   "Amount paid cannot exceed the amount due.");
    public static readonly Error AlreadyClosed     = new("IncomeInvoice.AlreadyClosed",     "Income invoice is already fully paid and closed.");
    public static readonly Error TransactionAmountInvalid = new("IncomeInvoice.TransactionAmountInvalid", "Transaction amount must be greater than zero and cannot exceed the remaining balance.");

    // ── Void / Refund ──────────────────────────────────────────────
    public static readonly Error InvoiceVoided       = new("IncomeInvoice.InvoiceVoided",       "Income invoice is voided and can no longer accept payments.");
    public static readonly Error AlreadyVoided       = new("IncomeInvoice.AlreadyVoided",       "Income invoice is already voided.");
    public static readonly Error VoidReasonRequired  = new("IncomeInvoice.VoidReasonRequired",  "A reason is required to void an invoice.");
    public static readonly Error TransactionNotFound = new("IncomeInvoice.TransactionNotFound", "Transaction not found on this invoice.");
    public static readonly Error TransactionNotPaid  = new("IncomeInvoice.TransactionNotPaid",  "Only a Paid transaction can be refunded.");
    public static readonly Error CannotRefundVoidedInvoice = new("IncomeInvoice.CannotRefundVoidedInvoice", "Cannot refund a transaction on a voided invoice — void the invoice instead.");
    public static readonly Error RefundAmountInvalid = new("IncomeInvoice.RefundAmountInvalid", "Refund amount must be greater than zero and cannot exceed the refundable balance of the transaction.");
    public static readonly Error RefundReasonRequired = new("IncomeInvoice.RefundReasonRequired", "A reason is required to refund a transaction.");
}
