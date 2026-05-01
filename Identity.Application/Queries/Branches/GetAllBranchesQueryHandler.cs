using Identity.Application.Dtos.Branches;
using Identity.Application.Mappings.Branches;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Branches;

public sealed record GetAllBranchesQuery(PagedRequest Request) : IQuery<PagedResult<BranchDto>>;

internal sealed class GetAllBranchesQueryHandler : IQueryHandler<GetAllBranchesQuery, PagedResult<BranchDto>>
{
    private readonly IBranchRepository _branchRepository;

    public GetAllBranchesQueryHandler(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<Result<PagedResult<BranchDto>>> Handle(GetAllBranchesQuery query, CancellationToken cancellationToken)
    {
        var result = await _branchRepository.GetPagedAsync(query.Request, cancellationToken);

        return Result.Success(PagedResult<BranchDto>.Create(
            result.Items.ToListDtos(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
