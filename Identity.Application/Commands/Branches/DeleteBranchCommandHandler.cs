using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Branches;

public sealed record DeleteBranchCommand(Guid BranchId) : ICommand;

internal sealed class DeleteBranchCommandHandler : ICommandHandler<DeleteBranchCommand>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteBranchCommandHandler> _logger;

    public DeleteBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork, ILogger<DeleteBranchCommandHandler> logger)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteBranch: starting for branch {BranchId}", request.BranchId);

        var branch = await _branchRepository.GetByIdIgnoringFiltersAsync(request.BranchId, cancellationToken);
        if (branch is null)
        {
            _logger.LogInformation("DeleteBranch: branch {BranchId} not found", request.BranchId);
            return Result.Failure(BranchErrors.NotFound);
        }

        branch.StatusId = (short)EntityStatusEnum.Inactive;
        branch.ModifiedOn = DateTimeOffset.UtcNow;

        _logger.LogInformation("DeleteBranch: deactivating branch {BranchId}", branch.Id);
        _branchRepository.Update(branch);

        _logger.LogInformation("DeleteBranch: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DeleteBranch: succeeded — branch {BranchId} deleted", branch.Id);
        return Result.Success();
    }
}
