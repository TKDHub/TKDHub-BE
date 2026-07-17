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

public sealed record VoidIncomeInvoiceCommand(VoidIncomeInvoiceModel Model) : ICommand<IncomeInvoiceDto>;

/// <summary>
/// Voids an invoice and, for every Paid transaction still holding an unrefunded
/// balance, creates a matching Refund transaction for that remaining balance.
/// The original Paid transactions are never mutated — refunds are new rows.
/// </summary>
internal sealed class VoidIncomeInvoiceCommandHandler : ICommandHandler<VoidIncomeInvoiceCommand, IncomeInvoiceDto>
{
    private readonly IIncomeInvoiceRepository _invoiceRepository;
    private readonly IStudentRepository       _studentRepository;
    private readonly IUnitOfWork              _unitOfWork;
    private readonly ILogger<VoidIncomeInvoiceCommandHandler> _logger;

    public VoidIncomeInvoiceCommandHandler(
        IIncomeInvoiceRepository invoiceRepository,
        IStudentRepository       studentRepository,
        IUnitOfWork              unitOfWork,
        ILogger<VoidIncomeInvoiceCommandHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _studentRepository = studentRepository;
        _unitOfWork        = unitOfWork;
        _logger             = logger;
    }

    public async Task<Result<IncomeInvoiceDto>> Handle(VoidIncomeInvoiceCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;
        _logger.LogInformation("VoidIncomeInvoice: starting for invoice {InvoiceId}", model.InvoiceId);

        var invoice = await _invoiceRepository.GetByIdAsync(model.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogInformation("VoidIncomeInvoice: invoice {InvoiceId} not found", model.InvoiceId);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.NotFound);
        }

        if (invoice.Status == IncomeInvoiceStatusEnum.Voided)
        {
            _logger.LogInformation("VoidIncomeInvoice: rejected — invoice {InvoiceId} already voided", invoice.Id);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.AlreadyVoided);
        }

        // Snapshot the Paid transactions before mutating Status, then cascade a refund
        // for whatever balance of each is not already offset by an earlier refund.
        var paidTransactions = invoice.Transactions
            .Where(t => t.Status == IncomeTransactionStatusEnum.Paid)
            .ToList();

        _logger.LogInformation("VoidIncomeInvoice: cascading refunds for {Count} paid transaction(s)", paidTransactions.Count);
        foreach (var paid in paidTransactions)
        {
            var alreadyRefunded = invoice.Transactions
                .Where(t => t.Status == IncomeTransactionStatusEnum.Refund && t.RefundOfTransactionId == paid.Id)
                .Sum(t => t.Amount);

            var refundable = paid.Amount - alreadyRefunded;
            if (refundable > 0)
            {
                _logger.LogInformation("VoidIncomeInvoice: refunding {Refundable} on transaction {TransactionId}", refundable, paid.Id);
                invoice.Transactions.Add(paid.ToRefundTransaction(
                    refundable, model.Reason, model.VoidedByEmail, model.VoidedByName));
            }
        }

        invoice.ApplyVoid(model);
        _logger.LogInformation("VoidIncomeInvoice: invoice {InvoiceId} marked voided", invoice.Id);

        // Voiding a subscription invoice cancels the membership it paid for — the student
        // is no longer active until they register a new one.
        if (invoice.Type == IncomeInvoiceTypeEnum.Subscription)
        {
            _logger.LogInformation("VoidIncomeInvoice: subscription invoice — deactivating student {StudentId}", invoice.Student.Id);
            invoice.Student.StatusId = (short)StudentStatusEnum.Inactive;
            _studentRepository.Update(invoice.Student);
        }

        _invoiceRepository.Update(invoice);

        _logger.LogInformation("VoidIncomeInvoice: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("VoidIncomeInvoice: succeeded — invoice {InvoiceId} voided", invoice.Id);
        return Result.Success(invoice.ToDto());
    }
}
