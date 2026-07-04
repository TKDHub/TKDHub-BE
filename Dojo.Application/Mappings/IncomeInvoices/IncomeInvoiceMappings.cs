using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Models.IncomeInvoice;
using Dojo.Domain.Entities;
using Dojo.Domain.Enums;

namespace Dojo.Application.Mappings.IncomeInvoices;

public static class IncomeInvoiceMappings
{
    /// <summary>
    /// Builds a new open invoice from the checkout model, snapshotting the branch from
    /// the student and the currency from the branch itself (fetched by the handler, so
    /// it reflects the branch's current currency rather than a stale student snapshot).
    /// Transactions are added by the handler.
    /// </summary>
    public static IncomeInvoice ToEntity(this CreateIncomeInvoiceModel model, Student student, string currency)
        => new()
        {
            BranchId       = student.BranchId,
            StudentId      = student.Id,
            Type           = model.Type,
            OriginalPrice  = model.OriginalPrice,
            DiscountType   = model.DiscountType,
            DiscountValue  = model.DiscountType is null ? 0m : model.DiscountValue,
            Currency       = currency,
            Status         = IncomeInvoiceStatusEnum.Open,
            CreatedOn      = DateTimeOffset.UtcNow,
            CreatedByEmail = model.CreatedByEmail,
            CreatedByName  = model.CreatedByName
        };

    /// <summary>
    /// Builds the first transaction at checkout from the create model, taking the
    /// branch from the invoice and the amount the caller decided to record.
    /// </summary>
    public static IncomeTransaction ToTransaction(this CreateIncomeInvoiceModel model, IncomeInvoice invoice, decimal amount)
        => new()
        {
            BranchId       = invoice.BranchId,
            Amount         = amount,
            Method         = model.PaymentMethod!.Value,
            CreatedOn      = DateTimeOffset.UtcNow,
            CreatedByEmail = model.CreatedByEmail,
            CreatedByName  = model.CreatedByName
        };

    /// <summary>Builds a subsequent transaction against an existing invoice.</summary>
    public static IncomeTransaction ToEntity(this AddIncomeTransactionModel model, IncomeInvoice invoice)
        => new()
        {
            BranchId        = invoice.BranchId,
            IncomeInvoiceId = invoice.Id,
            Amount          = model.Amount,
            Method          = model.Method,
            CreatedOn       = DateTimeOffset.UtcNow,
            CreatedByEmail  = model.CreatedByEmail,
            CreatedByName   = model.CreatedByName
        };

    /// <summary>Marks the invoice Voided and stamps who/when/why. Does not touch transactions.</summary>
    public static IncomeInvoice ApplyVoid(this IncomeInvoice invoice, VoidIncomeInvoiceModel model)
    {
        invoice.Status        = IncomeInvoiceStatusEnum.Voided;
        invoice.VoidedOn       = DateTimeOffset.UtcNow;
        invoice.VoidedByEmail  = model.VoidedByEmail;
        invoice.VoidedByName   = model.VoidedByName;
        invoice.VoidReason     = model.Reason;
        return invoice;
    }

    /// <summary>
    /// Builds a Refund transaction that offsets <paramref name="amount"/> of the given
    /// Paid transaction. Always a new row — the original Paid transaction is never mutated.
    /// </summary>
    public static IncomeTransaction ToRefundTransaction(
        this IncomeTransaction original,
        decimal amount,
        string  reason,
        string  byEmail,
        string  byName)
    {
        var now = DateTimeOffset.UtcNow;

        return new IncomeTransaction
        {
            BranchId              = original.BranchId,
            IncomeInvoiceId       = original.IncomeInvoiceId,
            Amount                = amount,
            Method                = original.Method,
            Status                = IncomeTransactionStatusEnum.Refund,
            RefundOfTransactionId = original.Id,
            RefundedOn            = now,
            RefundedByEmail       = byEmail,
            RefundedByName        = byName,
            RefundReason          = reason,
            CreatedOn             = now,
            CreatedByEmail        = byEmail,
            CreatedByName         = byName
        };
    }

    public static List<IncomeInvoiceDto> ToListDtos(this IEnumerable<IncomeInvoice> invoices)
        => invoices.Select(i => i.ToDto()).ToList();

    public static IncomeInvoiceDto ToDto(this IncomeInvoice invoice)
        => new()
        {
            Id          = invoice.Id,
            TenantId    = invoice.TenantId,
            BranchId    = invoice.BranchId,
            StudentId   = invoice.StudentId,
            StudentName = invoice.Student?.FullName,

            Type = invoice.Type.ToString(),

            OriginalPrice = invoice.OriginalPrice,
            DiscountType  = invoice.DiscountType?.ToString(),
            DiscountValue = invoice.DiscountValue,
            Currency      = invoice.Currency,

            DiscountAmount     = invoice.DiscountAmount,
            PriceAfterDiscount = invoice.PriceAfterDiscount,
            AmountPaid         = invoice.AmountPaid,
            RemainingAmount    = invoice.RemainingAmount,

            Status        = invoice.Status.ToString(),
            PaymentStatus = invoice.PaymentStatus.ToString(),

            VoidedOn      = invoice.VoidedOn,
            VoidedByEmail = invoice.VoidedByEmail,
            VoidedByName  = invoice.VoidedByName,
            VoidReason    = invoice.VoidReason,

            CreatedOn  = invoice.CreatedOn,
            ModifiedOn = invoice.ModifiedOn,

            Transactions = invoice.Transactions?
                .OrderBy(t => t.CreatedOn)
                .Select(t => t.ToDto())
                .ToList() ?? []
        };

    public static IncomeTransactionDto ToDto(this IncomeTransaction transaction)
        => new()
        {
            Id                    = transaction.Id,
            IncomeInvoiceId       = transaction.IncomeInvoiceId,
            Amount                = transaction.Amount,
            Method                = transaction.Method.ToString(),
            Status                = transaction.Status.ToString(),
            RefundOfTransactionId = transaction.RefundOfTransactionId,
            RefundedOn            = transaction.RefundedOn,
            RefundedByEmail       = transaction.RefundedByEmail,
            RefundedByName        = transaction.RefundedByName,
            RefundReason          = transaction.RefundReason,
            CreatedOn             = transaction.CreatedOn
        };
}
