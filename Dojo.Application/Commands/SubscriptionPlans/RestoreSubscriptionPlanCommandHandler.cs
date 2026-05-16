using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.SubscriptionPlans;

public sealed record RestoreSubscriptionPlanCommand(Guid PlanId) : ICommand;

internal sealed class RestoreSubscriptionPlanCommandHandler : ICommandHandler<RestoreSubscriptionPlanCommand>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IUnitOfWork                 _unitOfWork;

    public RestoreSubscriptionPlanCommandHandler(
        ISubscriptionPlanRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RestoreSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure(SubscriptionPlanErrors.NotFound);

        if (plan.StatusId == (short)EntityStatusEnum.Active)
            return Result.Failure(SubscriptionPlanErrors.AlreadyActive);

        plan.StatusId   = (short)EntityStatusEnum.Active;
        plan.ModifiedOn = DateTimeOffset.UtcNow;

        _repository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
