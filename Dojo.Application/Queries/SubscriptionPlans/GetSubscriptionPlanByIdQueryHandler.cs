using Shared.Application.Contracts;
using Dojo.Application.Dtos.SubscriptionPlans;
using Dojo.Application.Mappings.SubscriptionPlans;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.SubscriptionPlans;

public sealed record GetSubscriptionPlanByIdQuery(Guid PlanId) : IQuery<SubscriptionPlanDto>;

internal sealed class GetSubscriptionPlanByIdQueryHandler : IQueryHandler<GetSubscriptionPlanByIdQuery, SubscriptionPlanDto>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IBranchService              _branchService;

    public GetSubscriptionPlanByIdQueryHandler(
        ISubscriptionPlanRepository repository,
        IBranchService branchService)
    {
        _repository    = repository;
        _branchService = branchService;
    }

    public async Task<Result<SubscriptionPlanDto>> Handle(GetSubscriptionPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(request.PlanId, cancellationToken);

        if (plan is null)
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NotFound);

        var currency = await _branchService.GetCurrencyAsync(plan.BranchId, cancellationToken);

        return Result.Success(plan.ToDto(currency ?? "N/A"));
    }
}
