using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Branches;

public sealed record DeleteBranchCommand(Guid BranchId, bool DeleteRecursively = false) : ICommand;

internal sealed class DeleteBranchCommandHandler : ICommandHandler<DeleteBranchCommand>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository.GetByIdIgnoringFiltersAsync(request.BranchId, cancellationToken);
        if (branch is null)
            return Result.Failure(BranchErrors.NotFound);

        branch.StatusId = (short)EntityStatusEnum.Inactive;
        branch.ModifiedOn = DateTimeOffset.UtcNow;

        _branchRepository.Update(branch);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
