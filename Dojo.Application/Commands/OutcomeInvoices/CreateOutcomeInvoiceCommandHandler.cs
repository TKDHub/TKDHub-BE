using Dojo.Application.Dtos.OutcomeInvoices;
using Dojo.Application.Mappings.OutcomeInvoices;
using Dojo.Application.Models.OutcomeInvoice;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<CreateOutcomeInvoiceCommandHandler> _logger;

    public CreateOutcomeInvoiceCommandHandler(
        IOutcomeInvoiceRepository repository,
        IBranchService branchService,
        IUnitOfWork unitOfWork,
        ILogger<CreateOutcomeInvoiceCommandHandler> logger)
    {
        _repository    = repository;
        _branchService = branchService;
        _unitOfWork    = unitOfWork;
        _logger         = logger;
    }

    public async Task<Result<OutcomeInvoiceDto>> Handle(CreateOutcomeInvoiceCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;
        _logger.LogInformation("CreateOutcomeInvoice: starting for branch {BranchId}, tenant {TenantId}", request.BranchId, request.TenantId);

        if (request.BranchId == Guid.Empty)
        {
            _logger.LogInformation("CreateOutcomeInvoice: rejected — branch id was empty");
            return Result.Failure<OutcomeInvoiceDto>(OutcomeInvoiceErrors.BranchRequired);
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            _logger.LogInformation("CreateOutcomeInvoice: rejected — title missing");
            return Result.Failure<OutcomeInvoiceDto>(OutcomeInvoiceErrors.TitleRequired);
        }

        if (model.Amount <= 0)
        {
            _logger.LogInformation("CreateOutcomeInvoice: rejected — amount invalid ({Amount})", model.Amount);
            return Result.Failure<OutcomeInvoiceDto>(OutcomeInvoiceErrors.AmountInvalid);
        }

        var branch = await _branchService.GetBranchAsync(request.BranchId, cancellationToken);
        if (branch is null)
        {
            _logger.LogInformation("CreateOutcomeInvoice: branch {BranchId} not found", request.BranchId);
            return Result.Failure<OutcomeInvoiceDto>(OutcomeInvoiceErrors.BranchNotFound);
        }

        if (branch.TenantId != request.TenantId)
        {
            _logger.LogInformation("CreateOutcomeInvoice: branch {BranchId} tenant mismatch", request.BranchId);
            return Result.Failure<OutcomeInvoiceDto>(OutcomeInvoiceErrors.TenantBranchMismatch);
        }

        var invoice = model.ToEntity(request.BranchId, request.TenantId, branch.Currency ?? "N/A", attachmentUrl: null);

        _logger.LogInformation("CreateOutcomeInvoice: adding invoice entity");
        _repository.Add(invoice);

        _logger.LogInformation("CreateOutcomeInvoice: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CreateOutcomeInvoice: succeeded — invoice {InvoiceId} created", invoice.Id);
        return Result.Success(invoice.ToDto());
    }
}
