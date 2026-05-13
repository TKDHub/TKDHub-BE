using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.SubscriptionPlans;

public sealed record ArchiveSubscriptionPlanCommand(Guid PlanId) : ICommand;

internal sealed class ArchiveSubscriptionPlanCommandHandler : ICommandHandler<ArchiveSubscriptionPlanCommand>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IUnitOfWork                 _unitOfWork;

    public ArchiveSubscriptionPlanCommandHandler(
        ISubscriptionPlanRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ArchiveSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(request.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure(SubscriptionPlanErrors.NotFound);

        if (plan.StatusId == (short)EntityStatusEnum.Inactive)
            return Result.Failure(SubscriptionPlanErrors.AlreadyArchived);

        plan.StatusId   = (short)EntityStatusEnum.Inactive;
        plan.ModifiedOn = DateTimeOffset.UtcNow;

        _repository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
