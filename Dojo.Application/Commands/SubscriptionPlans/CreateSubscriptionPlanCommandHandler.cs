using Dojo.Application.Dtos.SubscriptionPlans;
using Dojo.Application.Mappings.SubscriptionPlans;
using Dojo.Application.Models.SubscriptionPlan;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.SubscriptionPlans;

public sealed record CreateSubscriptionPlanCommand(SubscriptionPlanModel Model, Guid BranchId, Guid TenantId) : ICommand<SubscriptionPlanDto>;

internal sealed class CreateSubscriptionPlanCommandHandler : ICommandHandler<CreateSubscriptionPlanCommand, SubscriptionPlanDto>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IBranchService              _branchService;
    private readonly IUnitOfWork                 _unitOfWork;
    private readonly ILogger<CreateSubscriptionPlanCommandHandler> _logger;

    public CreateSubscriptionPlanCommandHandler(
        ISubscriptionPlanRepository repository,
        IBranchService branchService,
        IUnitOfWork unitOfWork,
        ILogger<CreateSubscriptionPlanCommandHandler> logger)
    {
        _repository    = repository;
        _branchService = branchService;
        _unitOfWork    = unitOfWork;
        _logger         = logger;
    }

    public async Task<Result<SubscriptionPlanDto>> Handle(CreateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateSubscriptionPlan: starting for branch {BranchId}, tenant {TenantId}", request.BranchId, request.TenantId);

        if (request.BranchId == Guid.Empty)
        {
            _logger.LogInformation("CreateSubscriptionPlan: rejected — branch id was empty");
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.BranchRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Model.Name))
        {
            _logger.LogInformation("CreateSubscriptionPlan: rejected — name missing");
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NameRequired);
        }

        if (request.Model.DurationMonths < 1)
        {
            _logger.LogInformation("CreateSubscriptionPlan: rejected — duration invalid ({Duration})", request.Model.DurationMonths);
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.DurationInvalid);
        }

        if (request.Model.Price < 0)
        {
            _logger.LogInformation("CreateSubscriptionPlan: rejected — price invalid ({Price})", request.Model.Price);
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.PriceInvalid);
        }

        // Verify branch exists and belongs to the requesting tenant
        var branch = await _branchService.GetBranchAsync(request.BranchId, cancellationToken);

        if (branch is null)
        {
            _logger.LogInformation("CreateSubscriptionPlan: branch {BranchId} not found", request.BranchId);
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.BranchNotFound);
        }

        if (branch.TenantId != request.TenantId)
        {
            _logger.LogInformation("CreateSubscriptionPlan: branch {BranchId} tenant mismatch", request.BranchId);
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.TenantBranchMismatch);
        }

        var nameExists = await _repository.ExistsByNameAsync(
            request.Model.Name, request.BranchId, null, cancellationToken);

        if (nameExists)
        {
            _logger.LogInformation("CreateSubscriptionPlan: rejected — name {Name} already exists in branch {BranchId}", request.Model.Name, request.BranchId);
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NameAlreadyExists);
        }

        var plan = request.Model.ToEntity(request.BranchId, request.TenantId);

        _logger.LogInformation("CreateSubscriptionPlan: adding plan entity");
        _repository.Add(plan);

        _logger.LogInformation("CreateSubscriptionPlan: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CreateSubscriptionPlan: succeeded — plan {PlanId} created", plan.Id);
        return Result.Success(plan.ToDto(branch.Currency ?? "N/A"));
    }
}
