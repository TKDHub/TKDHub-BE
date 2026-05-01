using Identity.Application.Dtos.Branches;
using Identity.Application.Mappings.Branches;
using Identity.Application.Models.Branch;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Branches;

public sealed record CreateBranchCommand(BranchModel Model) : ICommand<BranchDto>;

internal sealed class CreateBranchCommandHandler : ICommandHandler<CreateBranchCommand, BranchDto>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BranchDto>> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Model.Name))
            return Result.Failure<BranchDto>(BranchErrors.NameRequired);

        if (string.IsNullOrWhiteSpace(request.Model.Email))
            return Result.Failure<BranchDto>(BranchErrors.EmailRequired);

        var nameExists = await _branchRepository.ExistsByNameAsync(request.Model.Name, null, cancellationToken);
        if (nameExists)
            return Result.Failure<BranchDto>(BranchErrors.NameAlreadyExists);

        var branch = request.Model.ToEntity();

        _branchRepository.Add(branch);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(branch.ToDto());
    }
}
