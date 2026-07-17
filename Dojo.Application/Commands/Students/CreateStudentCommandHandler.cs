using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Application.Models.Student;
using Dojo.Domain.Constants;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Students;

public sealed record CreateStudentCommand(StudentModel Model, Guid BranchId, Guid TenantId) : ICommand<StudentDto>;

internal sealed class CreateStudentCommandHandler : ICommandHandler<CreateStudentCommand, StudentDto>
{
    private readonly IStudentRepository            _studentRepository;
    private readonly ISubscriptionPlanRepository   _planRepository;
    private readonly IBranchService                _branchService;
    private readonly IStudentActivityLogRepository _activityLogRepository;
    private readonly IUnitOfWork                   _unitOfWork;
    private readonly ILogger<CreateStudentCommandHandler> _logger;

    public CreateStudentCommandHandler(
        IStudentRepository            studentRepository,
        ISubscriptionPlanRepository   planRepository,
        IBranchService                branchService,
        IStudentActivityLogRepository activityLogRepository,
        IUnitOfWork                   unitOfWork,
        ILogger<CreateStudentCommandHandler> logger)
    {
        _studentRepository      = studentRepository;
        _planRepository         = planRepository;
        _branchService          = branchService;
        _activityLogRepository  = activityLogRepository;
        _unitOfWork             = unitOfWork;
        _logger                 = logger;
    }

    public async Task<Result<StudentDto>> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateStudent: starting for branch {BranchId}, tenant {TenantId}", request.BranchId, request.TenantId);

        if (request.BranchId == Guid.Empty)
        {
            _logger.LogInformation("CreateStudent: rejected — branch id was empty");
            return Result.Failure<StudentDto>(StudentErrors.BranchRequired);
        }

        _logger.LogInformation("CreateStudent: looking up branch {BranchId}", request.BranchId);
        var branch = await _branchService.GetBranchAsync(request.BranchId, cancellationToken);
        if (branch is null)
        {
            _logger.LogInformation("CreateStudent: branch {BranchId} not found", request.BranchId);
            return Result.Failure<StudentDto>(StudentErrors.BranchNotFound);
        }

        if (branch.TenantId != request.TenantId)
        {
            _logger.LogInformation("CreateStudent: branch {BranchId} belongs to tenant {ActualTenantId}, not requested tenant {RequestedTenantId}",
                request.BranchId, branch.TenantId, request.TenantId);
            return Result.Failure<StudentDto>(StudentErrors.TenantBranchMismatch);
        }

        if (string.IsNullOrWhiteSpace(request.Model.FirstName))
        {
            _logger.LogInformation("CreateStudent: rejected — first name missing");
            return Result.Failure<StudentDto>(StudentErrors.FirstNameRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Model.LastName))
        {
            _logger.LogInformation("CreateStudent: rejected — last name missing");
            return Result.Failure<StudentDto>(StudentErrors.LastNameRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Model.PhoneNumber))
        {
            _logger.LogInformation("CreateStudent: rejected — phone number missing");
            return Result.Failure<StudentDto>(StudentErrors.PhoneRequired);
        }

        if (!string.IsNullOrWhiteSpace(request.Model.Email))
        {
            _logger.LogInformation("CreateStudent: checking email uniqueness");
            var emailExists = await _studentRepository.ExistsByEmailAsync(
                request.Model.Email, null, cancellationToken);
            if (emailExists)
            {
                _logger.LogInformation("CreateStudent: rejected — email already registered");
                return Result.Failure<StudentDto>(StudentErrors.EmailAlreadyExists);
            }
        }

        if (request.Model.SubscriptionPlanId == Guid.Empty)
        {
            _logger.LogInformation("CreateStudent: rejected — subscription plan id was empty");
            return Result.Failure<StudentDto>(StudentErrors.SubscriptionRequired);
        }

        _logger.LogInformation("CreateStudent: looking up subscription plan {PlanId}", request.Model.SubscriptionPlanId);
        var plan = await _planRepository.GetByIdAsync(request.Model.SubscriptionPlanId, cancellationToken);
        if (plan is null)
        {
            _logger.LogInformation("CreateStudent: subscription plan {PlanId} not found", request.Model.SubscriptionPlanId);
            return Result.Failure<StudentDto>(StudentErrors.NoActivePlans);
        }

        if (plan.StatusId != (short)EntityStatusEnum.Active)
        {
            _logger.LogInformation("CreateStudent: subscription plan {PlanId} is not active", plan.Id);
            return Result.Failure<StudentDto>(StudentErrors.SubscriptionNotActive);
        }

        // Snapshot plan terms + branch currency at the moment of registration
        var model = request.Model with
        {
            Price          = plan.Price,
            Currency       = branch.Currency ?? "N/A",
            DurationMonths = plan.DurationMonths
        };

        var student = model.ToEntity(request.BranchId, request.TenantId);

        _logger.LogInformation("CreateStudent: adding student entity");
        _studentRepository.Add(student);

        _logger.LogInformation("CreateStudent: writing activity log entry");
        _activityLogRepository.Add(StudentActivityLogMappings.NewLog(
            request.TenantId, request.BranchId, student.Id,
            StudentActivityType.Created, $"Student {student.FullName} was registered.",
            model.CreatedByEmail, model.CreatedByName));

        _logger.LogInformation("CreateStudent: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CreateStudent: succeeded — student {StudentId} created for branch {BranchId}", student.Id, request.BranchId);
        return Result.Success(student.ToDto());
    }
}
