using Dojo.Application.Dtos.SubscriptionPlans;
using Dojo.Application.Mappings.SubscriptionPlans;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<GetAllSubscriptionPlansQueryHandler> _logger;

    public GetAllSubscriptionPlansQueryHandler(
        ISubscriptionPlanRepository repository,
        IBranchContext branchContext,
        IBranchService branchService,
        IUserContext userContext,
        ILogger<GetAllSubscriptionPlansQueryHandler> logger)
    {
        _repository    = repository;
        _branchContext = branchContext;
        _branchService = branchService;
        _userContext   = userContext;
        _logger         = logger;
    }

    public async Task<Result<PagedResult<SubscriptionPlanDto>>> Handle(
        GetAllSubscriptionPlansQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllSubscriptionPlans: querying page {Page} size {PageSize}", request.Request.Page, request.Request.PageSize);

        var branch = await _branchService.GetBranchAsync(_branchContext.BranchId, cancellationToken);

        var branchId = _userContext.IsSuperAdmin ? (Guid?)null : _branchContext.BranchId;

        var result = await _repository.GetPagedAsync(request.Request, branchId, cancellationToken);

        _logger.LogInformation("GetAllSubscriptionPlans: returned {Count} of {Total} plan(s)", result.Items.Count, result.TotalCount);
        return Result.Success(PagedResult<SubscriptionPlanDto>.Create(
            result.Items.ToListDtos(branch?.Currency ?? "N/A"),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
