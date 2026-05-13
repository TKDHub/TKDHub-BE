using Dojo.Application.Dtos.SubscriptionPlans;
using Dojo.Application.Mappings.SubscriptionPlans;
using Dojo.Application.Models.SubscriptionPlan;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
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

    public CreateSubscriptionPlanCommandHandler(
        ISubscriptionPlanRepository repository,
        IBranchService branchService,
        IUnitOfWork unitOfWork)
    {
        _repository    = repository;
        _branchService = branchService;
        _unitOfWork    = unitOfWork;
    }

    public async Task<Result<SubscriptionPlanDto>> Handle(CreateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        if (request.BranchId == Guid.Empty)
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.BranchRequired);

        if (string.IsNullOrWhiteSpace(request.Model.Name))
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NameRequired);

        if (request.Model.DurationMonths < 1)
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.DurationInvalid);

        if (request.Model.Price < 0)
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.PriceInvalid);

        var nameExists = await _repository.ExistsByNameAsync(
            request.Model.Name, request.BranchId, null, cancellationToken);

        if (nameExists)
            return Result.Failure<SubscriptionPlanDto>(SubscriptionPlanErrors.NameAlreadyExists);

        var plan = request.Model.ToEntity(request.BranchId, request.TenantId);

        _repository.Add(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var currency = await _branchService.GetCurrencyAsync(request.BranchId, cancellationToken);

        return Result.Success(plan.ToDto(currency ?? "N/A"));
    }
}
