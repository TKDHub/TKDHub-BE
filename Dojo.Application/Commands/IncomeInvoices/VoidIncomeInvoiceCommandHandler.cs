using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Mappings.IncomeInvoices;
using Dojo.Application.Models.IncomeInvoice;
using Dojo.Domain.Constants;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
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
    private readonly IUnitOfWork              _unitOfWork;

    public VoidIncomeInvoiceCommandHandler(IIncomeInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork        = unitOfWork;
    }

    public async Task<Result<IncomeInvoiceDto>> Handle(VoidIncomeInvoiceCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;

        var invoice = await _invoiceRepository.GetByIdAsync(model.InvoiceId, cancellationToken);
        if (invoice is null)
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.NotFound);

        if (invoice.Status == IncomeInvoiceStatusEnum.Voided)
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.AlreadyVoided);

        // Snapshot the Paid transactions before mutating Status, then cascade a refund
        // for whatever balance of each is not already offset by an earlier refund.
        var paidTransactions = invoice.Transactions
            .Where(t => t.Status == IncomeTransactionStatusEnum.Paid)
            .ToList();

        foreach (var paid in paidTransactions)
        {
            var alreadyRefunded = invoice.Transactions
                .Where(t => t.Status == IncomeTransactionStatusEnum.Refund && t.RefundOfTransactionId == paid.Id)
                .Sum(t => t.Amount);

            var refundable = paid.Amount - alreadyRefunded;
            if (refundable > 0)
            {
                invoice.Transactions.Add(paid.ToRefundTransaction(
                    refundable, model.Reason, model.VoidedByEmail, model.VoidedByName));
            }
        }

        invoice.ApplyVoid(model);

        _invoiceRepository.Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(invoice.ToDto());
    }
}
