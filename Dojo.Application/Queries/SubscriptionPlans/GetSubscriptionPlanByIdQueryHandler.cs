using Shared.Application.Contracts;
using Dojo.Application.Dtos.SubscriptionPlans;
using Dojo.Application.Mappings.Students;
using Dojo.Application.Mappings.SubscriptionPlans;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.SubscriptionPlans;

public sealed record GetSubscriptionPlanByIdQuery(Guid PlanId) : IQuery<SubscriptionPlanDto>;

internal sealed class GetSubscriptionPlanByIdQueryHandler : IQueryHandler<GetSubscriptionPlanByIdQuery, SubscriptionPlanDto>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IBranchService              _branchService;
    private readonly ILogger<GetSubscriptionPlanByIdQueryHandler> _logger;

    public GetSubscriptionPlanByIdQueryHandler(
        ISubscriptionPlanRepository repository,
        IBranchService branchService,
        ILogger<GetSubscriptionPlanByIdQueryHandler> logger)
    {
        _repository    = repository;
        _branchService = branchService;
        _logger         = logger;
    }

    public async Task<Result<SubscriptionPlanDto>> Handle(GetSubscriptionPlanByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetSubscriptionPlanById: looking up plan {PlanId}", request.PlanId);

        var plan = await _repository.GetByIdWithStudentsAsync(request.PlanId, cancellationToken);

        if (plan is null)
        {
            _logger.LogInformation("GetSubscriptionPlanById: plan {PlanId} not found", request.PlanId);
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NotFound);
        }

        var branch      = await _branchService.GetBranchAsync(plan.BranchId, cancellationToken);
        var studentDtos = plan.Students.Select(s => s.ToDto()).ToList();

        _logger.LogInformation("GetSubscriptionPlanById: found plan {PlanId} with {StudentCount} student(s)", plan.Id, studentDtos.Count);
        return Result.Success(plan.ToDto(branch?.Currency ?? "N/A", studentDtos));
    }
}
