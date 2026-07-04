using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
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

    public DeleteOutcomeInvoiceCommandHandler(IOutcomeInvoiceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteOutcomeInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (invoice is null)
            return Result.Failure(OutcomeInvoiceErrors.NotFound);

        invoice.StatusId   = (short)EntityStatusEnum.Deleted;
        invoice.ModifiedOn = DateTimeOffset.UtcNow;

        _repository.Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
