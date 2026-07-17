using Shared.Application.Contracts;
using Dojo.Application.Dtos.SubscriptionPlans;
using Dojo.Application.Mappings.SubscriptionPlans;
using Dojo.Application.Models.SubscriptionPlan;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.SubscriptionPlans;

public sealed record UpdateSubscriptionPlanCommand(SubscriptionPlanModel Model) : ICommand<SubscriptionPlanDto>;

internal sealed class UpdateSubscriptionPlanCommandHandler : ICommandHandler<UpdateSubscriptionPlanCommand, SubscriptionPlanDto>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IBranchService              _branchService;
    private readonly IUnitOfWork                 _unitOfWork;
    private readonly ILogger<UpdateSubscriptionPlanCommandHandler> _logger;

    public UpdateSubscriptionPlanCommandHandler(
        ISubscriptionPlanRepository repository,
        IBranchService branchService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateSubscriptionPlanCommandHandler> logger)
    {
        _repository    = repository;
        _branchService = branchService;
        _unitOfWork    = unitOfWork;
        _logger         = logger;
    }

    public async Task<Result<SubscriptionPlanDto>> Handle(UpdateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateSubscriptionPlan: starting for plan {PlanId}", request.Model.PlanId);

        if (request.Model.PlanId is null)
        {
            _logger.LogInformation("UpdateSubscriptionPlan: rejected — plan id missing");
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NotFound);
        }

        if (string.IsNullOrWhiteSpace(request.Model.Name))
        {
            _logger.LogInformation("UpdateSubscriptionPlan: rejected — name missing");
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NameRequired);
        }

        if (request.Model.DurationMonths < 1)
        {
            _logger.LogInformation("UpdateSubscriptionPlan: rejected — duration invalid ({Duration})", request.Model.DurationMonths);
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.DurationInvalid);
        }

        if (request.Model.Price < 0)
        {
            _logger.LogInformation("UpdateSubscriptionPlan: rejected — price invalid ({Price})", request.Model.Price);
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.PriceInvalid);
        }

        var plan = await _repository.GetByIdAsync(request.Model.PlanId.Value, cancellationToken);
        if (plan is null)
        {
            _logger.LogInformation("UpdateSubscriptionPlan: plan {PlanId} not found", request.Model.PlanId);
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NotFound);
        }

        // Verify branch still exists and belongs to the plan's tenant
        var branch = await _branchService.GetBranchAsync(plan.BranchId, cancellationToken);

        if (branch is null)
        {
            _logger.LogInformation("UpdateSubscriptionPlan: branch {BranchId} not found", plan.BranchId);
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.BranchNotFound);
        }

        if (branch.TenantId != plan.TenantId)
        {
            _logger.LogInformation("UpdateSubscriptionPlan: branch {BranchId} tenant mismatch", plan.BranchId);
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.TenantBranchMismatch);
        }

        var nameExists = await _repository.ExistsByNameAsync(
            request.Model.Name, plan.BranchId, plan.Id, cancellationToken);

        if (nameExists)
        {
            _logger.LogInformation("UpdateSubscriptionPlan: rejected — name {Name} already exists in branch {BranchId}", request.Model.Name, plan.BranchId);
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NameAlreadyExists);
        }

        _logger.LogInformation("UpdateSubscriptionPlan: applying update to plan {PlanId}", plan.Id);
        plan.ApplyUpdate(request.Model);
        _repository.Update(plan);

        _logger.LogInformation("UpdateSubscriptionPlan: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UpdateSubscriptionPlan: succeeded — plan {PlanId} updated", plan.Id);
        return Result.Success(plan.ToDto(branch.Currency ?? "N/A"));
    }
}
