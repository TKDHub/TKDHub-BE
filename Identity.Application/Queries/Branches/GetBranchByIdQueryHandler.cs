using Identity.Application.Dtos.Branches;
using Identity.Application.Mappings.Branches;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Branches;

public sealed record GetBranchByIdQuery(Guid BranchId) : IQuery<BranchDto>;

internal sealed class GetBranchByIdQueryHandler : IQueryHandler<GetBranchByIdQuery, BranchDto>
{
    private readonly IBranchRepository _branchRepository;

    public GetBranchByIdQueryHandler(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<Result<BranchDto>> Handle(GetBranchByIdQuery query, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository.GetByIdAsync(query.BranchId, cancellationToken);
        if (branch is null)
            return Result.Failure<BranchDto>(BranchErrors.NotFound);

        return Result.Success(branch.ToDto());
    }
}
