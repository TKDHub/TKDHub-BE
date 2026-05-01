using Identity.Application.Dtos.Branches;
using Identity.Application.Mappings.Branches;
using Identity.Application.Models.Branch;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Branches;

public sealed record UpdateBranchCommand(BranchModel Model) : ICommand<BranchDto>;

internal sealed class UpdateBranchCommandHandler : ICommandHandler<UpdateBranchCommand, BranchDto>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateBranchCommandHandler> _logger;

    public UpdateBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork, ILogger<UpdateBranchCommandHandler> logger)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<BranchDto>> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Model.Name))
            return Result.Failure<BranchDto>(BranchErrors.NameRequired);

        if (string.IsNullOrWhiteSpace(request.Model.Email))
            return Result.Failure<BranchDto>(BranchErrors.EmailRequired);

        var branch = await _branchRepository.GetByIdIgnoringFiltersAsync(request.Model.BranchId!.Value, cancellationToken);
        if (branch is null)
            return Result.Failure<BranchDto>(BranchErrors.NotFound);

        var newName = request.Model.Name.Trim();
        if (!string.Equals(branch.Name, newName, StringComparison.OrdinalIgnoreCase))
        {
            var nameExists = await _branchRepository.ExistsByNameAsync(newName, branch.Id, cancellationToken);
            if (nameExists)
            {
                _logger.LogWarning("Branch name '{Name}' already in use — update rejected for branch {BranchId}", newName, branch.Id);
                return Result.Failure<BranchDto>(BranchErrors.NameAlreadyExists);
            }
        }

        branch.Name = newName;
        branch.Email = request.Model.Email.Trim().ToLowerInvariant();
        branch.PhoneNumber = request.Model.PhoneNumber?.Trim();
        branch.Enabled = request.Model.Enabled;
        branch.AddressCountry = request.Model.AddressCountry?.Trim();
        branch.AddressState = request.Model.AddressState?.Trim();
        branch.AddressCity = request.Model.AddressCity?.Trim();
        branch.AddressStreet = request.Model.AddressStreet?.Trim();
        branch.TimeZone = request.Model.TimeZone;
        branch.ModifiedOn = DateTimeOffset.UtcNow;
        branch.ModifiedByEmail = request.Model.ModifiedByEmail;
        branch.ModifiedByName = request.Model.ModifiedByName;

        _branchRepository.Update(branch);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Branch {BranchId} updated successfully", branch.Id);

        return Result.Success(branch.ToDto());
    }
}
