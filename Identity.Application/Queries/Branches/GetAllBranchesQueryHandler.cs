using Identity.Application.Dtos.Branches;
using Identity.Application.Mappings.Branches;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Branches;

public sealed record GetAllBranchesQuery(PagedRequest Request) : IQuery<PagedResult<BranchDto>>;

internal sealed class GetAllBranchesQueryHandler : IQueryHandler<GetAllBranchesQuery, PagedResult<BranchDto>>
{
    private readonly IBranchRepository _branchRepository;
    private readonly ILogger<GetAllBranchesQueryHandler> _logger;

    public GetAllBranchesQueryHandler(IBranchRepository branchRepository, ILogger<GetAllBranchesQueryHandler> logger)
    {
        _branchRepository = branchRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<BranchDto>>> Handle(GetAllBranchesQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllBranches: querying page {Page} size {PageSize}", query.Request.Page, query.Request.PageSize);

        var result = await _branchRepository.GetPagedAsync(query.Request, cancellationToken);

        _logger.LogInformation("GetAllBranches: returned {Count} of {Total} branch(es)", result.Items.Count, result.TotalCount);
        return Result.Success(PagedResult<BranchDto>.Create(
            result.Items.ToListDtos(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
