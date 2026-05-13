using Shared.Application.Contracts;
using Dojo.Application.Dtos.SubscriptionPlans;
using Dojo.Application.Mappings.SubscriptionPlans;
using Dojo.Application.Models.SubscriptionPlan;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.SubscriptionPlans;

public sealed record UpdateSubscriptionPlanCommand(SubscriptionPlanModel Model) : ICommand<SubscriptionPlanDto>;

internal sealed class UpdateSubscriptionPlanCommandHandler : ICommandHandler<UpdateSubscriptionPlanCommand, SubscriptionPlanDto>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IBranchService              _branchService;
    private readonly IUnitOfWork                 _unitOfWork;

    public UpdateSubscriptionPlanCommandHandler(
        ISubscriptionPlanRepository repository,
        IBranchService branchService,
        IUnitOfWork unitOfWork)
    {
        _repository    = repository;
        _branchService = branchService;
        _unitOfWork    = unitOfWork;
    }

    public async Task<Result<SubscriptionPlanDto>> Handle(UpdateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        if (request.Model.PlanId is null)
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NotFound);

        if (string.IsNullOrWhiteSpace(request.Model.Name))
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NameRequired);

        if (request.Model.DurationMonths < 1)
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.DurationInvalid);

        if (request.Model.Price < 0)
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.PriceInvalid);

        var plan = await _repository.GetByIdAsync(request.Model.PlanId.Value, cancellationToken);
        if (plan is null)
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NotFound);

        var nameExists = await _repository.ExistsByNameAsync(
            request.Model.Name, plan.BranchId, plan.Id, cancellationToken);

        if (nameExists)
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NameAlreadyExists);

        plan.ApplyUpdate(request.Model);
        _repository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var currency = await _branchService.GetCurrencyAsync(plan.BranchId, cancellationToken);

        return Result.Success(plan.ToDto(currency ?? "N/A"));
    }
}
