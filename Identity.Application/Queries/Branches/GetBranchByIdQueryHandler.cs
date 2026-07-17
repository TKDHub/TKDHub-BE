using Identity.Application.Dtos.Branches;
using Identity.Application.Mappings.Branches;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Branches;

public sealed record GetBranchByIdQuery(Guid BranchId) : IQuery<BranchDto>;

internal sealed class GetBranchByIdQueryHandler : IQueryHandler<GetBranchByIdQuery, BranchDto>
{
    private readonly IBranchRepository _branchRepository;
    private readonly ILogger<GetBranchByIdQueryHandler> _logger;

    public GetBranchByIdQueryHandler(IBranchRepository branchRepository, ILogger<GetBranchByIdQueryHandler> logger)
    {
        _branchRepository = branchRepository;
        _logger = logger;
    }

    public async Task<Result<BranchDto>> Handle(GetBranchByIdQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetBranchById: looking up branch {BranchId}", query.BranchId);

        var branch = await _branchRepository.GetByIdAsync(query.BranchId, cancellationToken);
        if (branch is null)
        {
            _logger.LogInformation("GetBranchById: branch {BranchId} not found", query.BranchId);
            return Result.Failure<BranchDto>(BranchErrors.NotFound);
        }

        _logger.LogInformation("GetBranchById: found branch {BranchId}", branch.Id);
        return Result.Success(branch.ToDto());
    }
}
