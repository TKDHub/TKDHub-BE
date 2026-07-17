using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Mappings.IncomeInvoices;
using Dojo.Application.Models.IncomeInvoice;
using Dojo.Domain.Constants;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<RefundIncomeTransactionCommandHandler> _logger;

    public RefundIncomeTransactionCommandHandler(IIncomeInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork, ILogger<RefundIncomeTransactionCommandHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork        = unitOfWork;
        _logger             = logger;
    }

    public async Task<Result<IncomeInvoiceDto>> Handle(RefundIncomeTransactionCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;
        _logger.LogInformation("RefundIncomeTransaction: starting for invoice {InvoiceId}, transaction {TransactionId}, amount {Amount}",
            model.InvoiceId, model.TransactionId, model.Amount);

        var invoice = await _invoiceRepository.GetByIdAsync(model.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogInformation("RefundIncomeTransaction: invoice {InvoiceId} not found", model.InvoiceId);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.NotFound);
        }

        if (invoice.Status == IncomeInvoiceStatusEnum.Voided)
        {
            _logger.LogInformation("RefundIncomeTransaction: rejected — invoice {InvoiceId} already voided", invoice.Id);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.CannotRefundVoidedInvoice);
        }

        var original = invoice.Transactions.FirstOrDefault(t => t.Id == model.TransactionId);
        if (original is null)
        {
            _logger.LogInformation("RefundIncomeTransaction: transaction {TransactionId} not found on invoice {InvoiceId}", model.TransactionId, invoice.Id);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.TransactionNotFound);
        }

        if (original.Status != IncomeTransactionStatusEnum.Paid)
        {
            _logger.LogInformation("RefundIncomeTransaction: rejected — transaction {TransactionId} is not Paid", original.Id);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.TransactionNotPaid);
        }

        var alreadyRefunded = invoice.Transactions
            .Where(t => t.Status == IncomeTransactionStatusEnum.Refund && t.RefundOfTransactionId == original.Id)
            .Sum(t => t.Amount);

        var refundable = original.Amount - alreadyRefunded;
        if (model.Amount <= 0 || model.Amount > refundable)
        {
            _logger.LogInformation("RefundIncomeTransaction: rejected — amount {Amount} invalid against refundable {Refundable}", model.Amount, refundable);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.RefundAmountInvalid);
        }

        _logger.LogInformation("RefundIncomeTransaction: adding refund transaction for {Amount}", model.Amount);
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
            _logger.LogInformation("RefundIncomeTransaction: all transactions now fully refunded — auto-voiding invoice {InvoiceId}", invoice.Id);
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
            _logger.LogInformation("RefundIncomeTransaction: invoice {InvoiceId} status re-derived to {Status}", invoice.Id, invoice.Status);
        }

        _invoiceRepository.Update(invoice);

        _logger.LogInformation("RefundIncomeTransaction: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("RefundIncomeTransaction: succeeded — invoice {InvoiceId}", invoice.Id);
        return Result.Success(invoice.ToDto());
    }
}
