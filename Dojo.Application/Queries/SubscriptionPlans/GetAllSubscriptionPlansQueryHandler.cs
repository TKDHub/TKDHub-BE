using Dojo.Application.Dtos.SubscriptionPlans;
using Dojo.Application.Mappings.SubscriptionPlans;
using Dojo.Domain.Repositories;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.SubscriptionPlans;

public sealed record GetAllSubscriptionPlansQuery(PagedRequest Request) : IQuery<PagedResult<SubscriptionPlanDto>>;

internal sealed class GetAllSubscriptionPlansQueryHandler : IQueryHandler<GetAllSubscriptionPlansQuery, PagedResult<SubscriptionPlanDto>>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IBranchContext              _branchContext;
    private readonly IBranchService              _branchService;
    private readonly IUserContext                _userContext;

    public GetAllSubscriptionPlansQueryHandler(
        ISubscriptionPlanRepository repository,
        IBranchContext branchContext,
        IBranchService branchService,
        IUserContext userContext)
    {
        _repository    = repository;
        _branchContext = branchContext;
        _branchService = branchService;
        _userContext   = userContext;
    }

    public async Task<Result<PagedResult<SubscriptionPlanDto>>> Handle(
        GetAllSubscriptionPlansQuery request,
        CancellationToken cancellationToken)
    {
        var branch = await _branchService.GetBranchAsync(_branchContext.BranchId, cancellationToken);

        var branchId = _userContext.IsSuperAdmin ? (Guid?)null : _branchContext.BranchId;

        var result = await _repository.GetPagedAsync(request.Request, branchId, cancellationToken);

        return Result.Success(PagedResult<SubscriptionPlanDto>.Create(
            result.Items.ToListDtos(branch?.Currency ?? "N/A"),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
