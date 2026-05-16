using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Application.Models.Student;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Students;

public sealed record CreateStudentCommand(StudentModel Model, Guid BranchId, Guid TenantId) : ICommand<StudentDto>;

internal sealed class CreateStudentCommandHandler : ICommandHandler<CreateStudentCommand, StudentDto>
{
    private readonly IStudentRepository          _studentRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IBranchService              _branchService;
    private readonly IUnitOfWork                 _unitOfWork;

    public CreateStudentCommandHandler(
        IStudentRepository          studentRepository,
        ISubscriptionPlanRepository planRepository,
        IBranchService              branchService,
        IUnitOfWork                 unitOfWork)
    {
        _studentRepository = studentRepository;
        _planRepository    = planRepository;
        _branchService     = branchService;
        _unitOfWork        = unitOfWork;
    }

    public async Task<Result<StudentDto>> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        if (request.BranchId == Guid.Empty)
            return Result.Failure<StudentDto>(StudentErrors.BranchRequired);

        // Verify branch exists and belongs to the requesting tenant
        var branch = await _branchService.GetBranchAsync(request.BranchId, cancellationToken);

        if (branch is null)
            return Result.Failure<StudentDto>(StudentErrors.BranchNotFound);

        if (branch.TenantId != request.TenantId)
            return Result.Failure<StudentDto>(StudentErrors.TenantBranchMismatch);

        if (string.IsNullOrWhiteSpace(request.Model.FirstName))
            return Result.Failure<StudentDto>(StudentErrors.FirstNameRequired);

        if (string.IsNullOrWhiteSpace(request.Model.LastName))
            return Result.Failure<StudentDto>(StudentErrors.LastNameRequired);

        if (string.IsNullOrWhiteSpace(request.Model.PhoneNumber))
            return Result.Failure<StudentDto>(StudentErrors.PhoneRequired);

        // Email uniqueness — only when provided
        if (!string.IsNullOrWhiteSpace(request.Model.Email))
        {
            var emailExists = await _studentRepository.ExistsByEmailAsync(
                request.Model.Email, null, cancellationToken);

            if (emailExists)
                return Result.Failure<StudentDto>(StudentErrors.EmailAlreadyExists);
        }

        // Subscription plan — must exist and be Active in the current branch
        if (request.Model.SubscriptionPlanId == Guid.Empty)
            return Result.Failure<StudentDto>(StudentErrors.SubscriptionRequired);

        var plan = await _planRepository.GetByIdAsync(request.Model.SubscriptionPlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<StudentDto>(StudentErrors.NoActivePlans);

        if (plan.StatusId != (short)EntityStatusEnum.Active)
            return Result.Failure<StudentDto>(StudentErrors.SubscriptionNotActive);

        // Snapshot plan terms + branch currency at the moment of registration.
        // These values are frozen on the student record and never change when the plan or
        // branch currency is later updated.
        var model = request.Model with
        {
            Price          = plan.Price,
            Currency       = branch.Currency ?? "N/A",
            DurationMonths = plan.DurationMonths
        };

        var student = model.ToEntity(request.BranchId, request.TenantId);

        _studentRepository.Add(student);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(student.ToDto());
    }
}
