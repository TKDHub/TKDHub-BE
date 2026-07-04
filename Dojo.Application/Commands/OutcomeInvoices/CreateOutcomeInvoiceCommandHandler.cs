using Dojo.Application.Dtos.OutcomeInvoices;
using Dojo.Application.Mappings.OutcomeInvoices;
using Dojo.Application.Models.OutcomeInvoice;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.OutcomeInvoices;

public sealed record CreateOutcomeInvoiceCommand(
    CreateOutcomeInvoiceModel Model,
    Guid BranchId,
    Guid TenantId) : ICommand<OutcomeInvoiceDto>;

internal sealed class CreateOutcomeInvoiceCommandHandler : ICommandHandler<CreateOutcomeInvoiceCommand, OutcomeInvoiceDto>
{
    private readonly IOutcomeInvoiceRepository _repository;
    private readonly IBranchService            _branchService;
    private readonly IUnitOfWork                _unitOfWork;

    public CreateOutcomeInvoiceCommandHandler(
        IOutcomeInvoiceRepository repository,
        IBranchService branchService,
        IUnitOfWork unitOfWork)
    {
        _repository    = repository;
        _branchService = branchService;
        _unitOfWork    = unitOfWork;
    }

    public async Task<Result<OutcomeInvoiceDto>> Handle(CreateOutcomeInvoiceCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;

        if (request.BranchId == Guid.Empty)
            return Result.Failure<OutcomeInvoiceDto>(OutcomeInvoiceErrors.BranchRequired);

        if (string.IsNullOrWhiteSpace(model.Title))
            return Result.Failure<OutcomeInvoiceDto>(OutcomeInvoiceErrors.TitleRequired);

        if (model.Amount <= 0)
            return Result.Failure<OutcomeInvoiceDto>(OutcomeInvoiceErrors.AmountInvalid);

        var branch = await _branchService.GetBranchAsync(request.BranchId, cancellationToken);
        if (branch is null)
            return Result.Failure<OutcomeInvoiceDto>(OutcomeInvoiceErrors.BranchNotFound);

        if (branch.TenantId != request.TenantId)
            return Result.Failure<OutcomeInvoiceDto>(OutcomeInvoiceErrors.TenantBranchMismatch);

        var invoice = model.ToEntity(request.BranchId, request.TenantId, branch.Currency ?? "N/A", attachmentUrl: null);

        _repository.Add(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(invoice.ToDto());
    }
}
