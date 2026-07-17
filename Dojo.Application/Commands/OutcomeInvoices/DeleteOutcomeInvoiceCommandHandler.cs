using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.OutcomeInvoices;

public sealed record DeleteOutcomeInvoiceCommand(Guid Id) : ICommand;

/// <summary>Soft-deletes an outcome invoice — the row is marked Deleted, never physically removed.</summary>
internal sealed class DeleteOutcomeInvoiceCommandHandler : ICommandHandler<DeleteOutcomeInvoiceCommand>
{
    private readonly IOutcomeInvoiceRepository _repository;
    private readonly IUnitOfWork               _unitOfWork;
    private readonly ILogger<DeleteOutcomeInvoiceCommandHandler> _logger;

    public DeleteOutcomeInvoiceCommandHandler(IOutcomeInvoiceRepository repository, IUnitOfWork unitOfWork, ILogger<DeleteOutcomeInvoiceCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger      = logger;
    }

    public async Task<Result> Handle(DeleteOutcomeInvoiceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteOutcomeInvoice: starting for invoice {InvoiceId}", request.Id);

        var invoice = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (invoice is null)
        {
            _logger.LogInformation("DeleteOutcomeInvoice: invoice {InvoiceId} not found", request.Id);
            return Result.Failure(OutcomeInvoiceErrors.NotFound);
        }

        invoice.StatusId   = (short)EntityStatusEnum.Deleted;
        invoice.ModifiedOn = DateTimeOffset.UtcNow;

        _logger.LogInformation("DeleteOutcomeInvoice: soft-deleting invoice {InvoiceId}", invoice.Id);
        _repository.Update(invoice);

        _logger.LogInformation("DeleteOutcomeInvoice: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DeleteOutcomeInvoice: succeeded — invoice {InvoiceId} deleted", invoice.Id);
        return Result.Success();
    }
}
