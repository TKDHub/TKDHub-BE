using Identity.Application.Dtos.Branches;
using Identity.Application.Mappings.Branches;
using Identity.Application.Models.Branch;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Branches;

public sealed record CreateBranchCommand(BranchModel Model) : ICommand<BranchDto>;

internal sealed class CreateBranchCommandHandler : ICommandHandler<CreateBranchCommand, BranchDto>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateBranchCommandHandler> _logger;

    public CreateBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork, ILogger<CreateBranchCommandHandler> logger)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<BranchDto>> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateBranch: starting for name {Name}", request.Model.Name);

        if (string.IsNullOrWhiteSpace(request.Model.Name))
        {
            _logger.LogInformation("CreateBranch: rejected — name missing");
            return Result.Failure<BranchDto>(BranchErrors.NameRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Model.Email))
        {
            _logger.LogInformation("CreateBranch: rejected — email missing");
            return Result.Failure<BranchDto>(BranchErrors.EmailRequired);
        }

        var nameExists = await _branchRepository.ExistsByNameAsync(request.Model.Name, null, cancellationToken);
        if (nameExists)
        {
            _logger.LogInformation("CreateBranch: rejected — name {Name} already exists", request.Model.Name);
            return Result.Failure<BranchDto>(BranchErrors.NameAlreadyExists);
        }

        var branch = request.Model.ToEntity();

        _logger.LogInformation("CreateBranch: adding branch entity");
        _branchRepository.Add(branch);

        _logger.LogInformation("CreateBranch: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CreateBranch: succeeded — branch {BranchId} created", branch.Id);
        return Result.Success(branch.ToDto());
    }
}
