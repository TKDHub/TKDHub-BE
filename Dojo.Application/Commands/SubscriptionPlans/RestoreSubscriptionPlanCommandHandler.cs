using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.SubscriptionPlans;

public sealed record RestoreSubscriptionPlanCommand(Guid PlanId) : ICommand;

internal sealed class RestoreSubscriptionPlanCommandHandler : ICommandHandler<RestoreSubscriptionPlanCommand>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IUnitOfWork                 _unitOfWork;
    private readonly ILogger<RestoreSubscriptionPlanCommandHandler> _logger;

    public RestoreSubscriptionPlanCommandHandler(
        ISubscriptionPlanRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<RestoreSubscriptionPlanCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger      = logger;
    }

    public async Task<Result> Handle(RestoreSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("RestoreSubscriptionPlan: starting for plan {PlanId}", request.PlanId);

        var plan = await _repository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null)
        {
            _logger.LogInformation("RestoreSubscriptionPlan: plan {PlanId} not found", request.PlanId);
            return Result.Failure(SubscriptionPlanErrors.NotFound);
        }

        if (plan.StatusId == (short)EntityStatusEnum.Active)
        {
            _logger.LogInformation("RestoreSubscriptionPlan: plan {PlanId} already active", plan.Id);
            return Result.Failure(SubscriptionPlanErrors.AlreadyActive);
        }

        plan.StatusId   = (short)EntityStatusEnum.Active;
        plan.ModifiedOn = DateTimeOffset.UtcNow;

        _logger.LogInformation("RestoreSubscriptionPlan: restoring plan {PlanId}", plan.Id);
        _repository.Update(plan);

        _logger.LogInformation("RestoreSubscriptionPlan: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("RestoreSubscriptionPlan: succeeded — plan {PlanId} restored", plan.Id);
        return Result.Success();
    }
}
