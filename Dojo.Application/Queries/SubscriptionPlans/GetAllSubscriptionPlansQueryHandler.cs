using Dojo.Application.Dtos.SubscriptionPlans;
using Dojo.Application.Mappings.SubscriptionPlans;
using Dojo.Domain.Repositories;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.SubscriptionPlans;

public sealed record GetAllSubscriptionPlansQuery(PagedRequest Request, string? Status) : IQuery<PagedResult<SubscriptionPlanDto>>;

internal sealed class GetAllSubscriptionPlansQueryHandler : IQueryHandler<GetAllSubscriptionPlansQuery, PagedResult<SubscriptionPlanDto>>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IBranchContext              _branchContext;
    private readonly IBranchService              _branchService;

    public GetAllSubscriptionPlansQueryHandler(
        ISubscriptionPlanRepository repository,
        IBranchContext branchContext,
        IBranchService branchService)
    {
        _repository    = repository;
        _branchContext = branchContext;
        _branchService = branchService;
    }

    public async Task<Result<PagedResult<SubscriptionPlanDto>>> Handle(
        GetAllSubscriptionPlansQuery request,
        CancellationToken cancellationToken)
    {
        var currency = await _branchService.GetCurrencyAsync(_branchContext.BranchId, cancellationToken);

        var result = await _repository.GetPagedAsync(request.Request, request.Status, cancellationToken);

        return Result.Success(PagedResult<SubscriptionPlanDto>.Create(
            result.Items.ToListDtos(currency ?? "N/A"),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
