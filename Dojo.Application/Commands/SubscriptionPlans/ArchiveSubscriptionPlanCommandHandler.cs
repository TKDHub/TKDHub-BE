using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.SubscriptionPlans;

public sealed record ArchiveSubscriptionPlanCommand(Guid PlanId) : ICommand;

internal sealed class ArchiveSubscriptionPlanCommandHandler : ICommandHandler<ArchiveSubscriptionPlanCommand>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IUnitOfWork                 _unitOfWork;
    private readonly ILogger<ArchiveSubscriptionPlanCommandHandler> _logger;

    public ArchiveSubscriptionPlanCommandHandler(
        ISubscriptionPlanRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ArchiveSubscriptionPlanCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger      = logger;
    }

    public async Task<Result> Handle(ArchiveSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ArchiveSubscriptionPlan: starting for plan {PlanId}", request.PlanId);

        var plan = await _repository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null)
        {
            _logger.LogInformation("ArchiveSubscriptionPlan: plan {PlanId} not found", request.PlanId);
            return Result.Failure(SubscriptionPlanErrors.NotFound);
        }

        if (plan.StatusId == (short)EntityStatusEnum.Inactive)
        {
            _logger.LogInformation("ArchiveSubscriptionPlan: plan {PlanId} already archived", plan.Id);
            return Result.Failure(SubscriptionPlanErrors.AlreadyArchived);
        }

        plan.StatusId   = (short)EntityStatusEnum.Inactive;
        plan.ModifiedOn = DateTimeOffset.UtcNow;

        _logger.LogInformation("ArchiveSubscriptionPlan: archiving plan {PlanId}", plan.Id);
        _repository.Update(plan);

        _logger.LogInformation("ArchiveSubscriptionPlan: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("ArchiveSubscriptionPlan: succeeded — plan {PlanId} archived", plan.Id);
        return Result.Success();
    }
}
