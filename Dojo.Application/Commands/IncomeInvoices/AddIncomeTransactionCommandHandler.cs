using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Mappings.IncomeInvoices;
using Dojo.Application.Models.IncomeInvoice;
using Dojo.Domain.Constants;
using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.IncomeInvoices;

public sealed record AddIncomeTransactionCommand(AddIncomeTransactionModel Model) : ICommand<IncomeInvoiceDto>;

internal sealed class AddIncomeTransactionCommandHandler : ICommandHandler<AddIncomeTransactionCommand, IncomeInvoiceDto>
{
    private readonly IIncomeInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork              _unitOfWork;
    private readonly ILogger<AddIncomeTransactionCommandHandler> _logger;

    public AddIncomeTransactionCommandHandler(IIncomeInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork, ILogger<AddIncomeTransactionCommandHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork        = unitOfWork;
        _logger             = logger;
    }

    public async Task<Result<IncomeInvoiceDto>> Handle(AddIncomeTransactionCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;
        _logger.LogInformation("AddIncomeTransaction: starting for invoice {InvoiceId}, amount {Amount}", model.IncomeInvoiceId, model.Amount);

        var invoice = await _invoiceRepository.GetByIdAsync(model.IncomeInvoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogInformation("AddIncomeTransaction: invoice {InvoiceId} not found", model.IncomeInvoiceId);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.NotFound);
        }

        if (invoice.Status == IncomeInvoiceStatusEnum.Voided)
        {
            _logger.LogInformation("AddIncomeTransaction: rejected — invoice {InvoiceId} is voided", invoice.Id);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.InvoiceVoided);
        }

        if (invoice.Status == IncomeInvoiceStatusEnum.Closed)
        {
            _logger.LogInformation("AddIncomeTransaction: rejected — invoice {InvoiceId} already closed", invoice.Id);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.AlreadyClosed);
        }

        if (model.Amount <= 0 || model.Amount > invoice.RemainingAmount)
        {
            _logger.LogInformation("AddIncomeTransaction: rejected — amount {Amount} invalid against remaining {Remaining}", model.Amount, invoice.RemainingAmount);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.TransactionAmountInvalid);
        }

        _logger.LogInformation("AddIncomeTransaction: adding transaction to invoice {InvoiceId}", invoice.Id);
        invoice.Transactions.Add(model.ToEntity(invoice));

        // Derive the new lifecycle state — close once fully covered.
        if (invoice.RemainingAmount <= 0)
        {
            _logger.LogInformation("AddIncomeTransaction: invoice {InvoiceId} fully covered — closing", invoice.Id);
            invoice.Status = IncomeInvoiceStatusEnum.Closed;
        }

        _invoiceRepository.Update(invoice);

        _logger.LogInformation("AddIncomeTransaction: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AddIncomeTransaction: succeeded — invoice {InvoiceId} now {Status}", invoice.Id, invoice.Status);
        return Result.Success(invoice.ToDto());
    }
}
