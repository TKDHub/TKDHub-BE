using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Mappings.IncomeInvoices;
using Dojo.Application.Models.IncomeInvoice;
using Dojo.Domain.Constants;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.IncomeInvoices;

public sealed record RefundIncomeTransactionCommand(RefundIncomeTransactionModel Model) : ICommand<IncomeInvoiceDto>;

/// <summary>
/// Refunds part or all of a single Paid transaction on an invoice. Creates a new
/// Refund transaction — the original Paid transaction is never mutated. Re-derives
/// the invoice's Open/Closed state afterward (a refund can reopen a Closed invoice) —
/// unless every Paid transaction on the invoice is now fully refunded, in which case
/// the invoice is auto-voided instead.
/// </summary>
internal sealed class RefundIncomeTransactionCommandHandler : ICommandHandler<RefundIncomeTransactionCommand, IncomeInvoiceDto>
{
    private readonly IIncomeInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork              _unitOfWork;

    public RefundIncomeTransactionCommandHandler(IIncomeInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork        = unitOfWork;
    }

    public async Task<Result<IncomeInvoiceDto>> Handle(RefundIncomeTransactionCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;

        var invoice = await _invoiceRepository.GetByIdAsync(model.InvoiceId, cancellationToken);
        if (invoice is null)
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.NotFound);

        if (invoice.Status == IncomeInvoiceStatusEnum.Voided)
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.CannotRefundVoidedInvoice);

        var original = invoice.Transactions.FirstOrDefault(t => t.Id == model.TransactionId);
        if (original is null)
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.TransactionNotFound);

        if (original.Status != IncomeTransactionStatusEnum.Paid)
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.TransactionNotPaid);

        var alreadyRefunded = invoice.Transactions
            .Where(t => t.Status == IncomeTransactionStatusEnum.Refund && t.RefundOfTransactionId == original.Id)
            .Sum(t => t.Amount);

        var refundable = original.Amount - alreadyRefunded;
        if (model.Amount <= 0 || model.Amount > refundable)
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.RefundAmountInvalid);

        invoice.Transactions.Add(original.ToRefundTransaction(
            model.Amount, model.Reason, model.RefundedByEmail, model.RefundedByName));

        // If every Paid transaction on the invoice is now fully refunded, there's
        // nothing left to collect on it — void it instead of just reopening/closing.
        var allPaidFullyRefunded = invoice.Transactions
            .Where(t => t.Status == IncomeTransactionStatusEnum.Paid)
            .All(paid =>
            {
                var refunded = invoice.Transactions
                    .Where(t => t.Status == IncomeTransactionStatusEnum.Refund && t.RefundOfTransactionId == paid.Id)
                    .Sum(t => t.Amount);
                return refunded >= paid.Amount;
            });

        if (allPaidFullyRefunded)
        {
            invoice.ApplyVoid(new VoidIncomeInvoiceModel
            {
                InvoiceId     = invoice.Id,
                Reason        = $"Auto-voided: all transactions refunded ({model.Reason})",
                VoidedByEmail = model.RefundedByEmail,
                VoidedByName  = model.RefundedByName
            });
        }
        else
        {
            // A refund can bring an already-Closed invoice back into an Open balance.
            invoice.Status = invoice.RemainingAmount <= 0
                ? IncomeInvoiceStatusEnum.Closed
                : IncomeInvoiceStatusEnum.Open;
        }

        _invoiceRepository.Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(invoice.ToDto());
    }
}
