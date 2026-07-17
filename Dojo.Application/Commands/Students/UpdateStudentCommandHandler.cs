using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Application.Models.Student;
using Dojo.Domain.Constants;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Students;

public sealed record UpdateStudentCommand(StudentModel Model) : ICommand<StudentDto>;

internal sealed class UpdateStudentCommandHandler : ICommandHandler<UpdateStudentCommand, StudentDto>
{
    private readonly IStudentRepository            _studentRepository;
    private readonly ISubscriptionPlanRepository   _planRepository;
    private readonly IStudentActivityLogRepository _activityLogRepository;
    private readonly IUnitOfWork                   _unitOfWork;
    private readonly ILogger<UpdateStudentCommandHandler> _logger;

    public UpdateStudentCommandHandler(
        IStudentRepository            studentRepository,
        ISubscriptionPlanRepository   planRepository,
        IStudentActivityLogRepository activityLogRepository,
        IUnitOfWork                   unitOfWork,
        ILogger<UpdateStudentCommandHandler> logger)
    {
        _studentRepository      = studentRepository;
        _planRepository         = planRepository;
        _activityLogRepository  = activityLogRepository;
        _unitOfWork             = unitOfWork;
        _logger                 = logger;
    }

    public async Task<Result<StudentDto>> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateStudent: starting for student {StudentId}", request.Model.StudentId);

        if (string.IsNullOrWhiteSpace(request.Model.FirstName))
        {
            _logger.LogInformation("UpdateStudent: rejected — first name missing");
            return Result.Failure<StudentDto>(StudentErrors.FirstNameRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Model.LastName))
        {
            _logger.LogInformation("UpdateStudent: rejected — last name missing");
            return Result.Failure<StudentDto>(StudentErrors.LastNameRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Model.PhoneNumber))
        {
            _logger.LogInformation("UpdateStudent: rejected — phone number missing");
            return Result.Failure<StudentDto>(StudentErrors.PhoneRequired);
        }

        _logger.LogInformation("UpdateStudent: looking up student {StudentId}", request.Model.StudentId);
        var student = await _studentRepository.GetByIdIgnoringFiltersAsync(
            request.Model.StudentId!.Value, cancellationToken);

        if (student is null)
        {
            _logger.LogInformation("UpdateStudent: student {StudentId} not found", request.Model.StudentId);
            return Result.Failure<StudentDto>(StudentErrors.NotFound);
        }

        if (!string.IsNullOrWhiteSpace(request.Model.Email))
        {
            var newEmail = request.Model.Email.Trim().ToLowerInvariant();
            if (!string.Equals(student.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("UpdateStudent: email changed, checking uniqueness");
                var emailExists = await _studentRepository.ExistsByEmailAsync(newEmail, student.Id, cancellationToken);
                if (emailExists)
                {
                    _logger.LogInformation("UpdateStudent: rejected — new email already registered");
                    return Result.Failure<StudentDto>(StudentErrors.EmailAlreadyExists);
                }
            }
        }

        if (request.Model.SubscriptionPlanId == Guid.Empty)
        {
            _logger.LogInformation("UpdateStudent: rejected — subscription plan id was empty");
            return Result.Failure<StudentDto>(StudentErrors.SubscriptionRequired);
        }

        _logger.LogInformation("UpdateStudent: looking up subscription plan {PlanId}", request.Model.SubscriptionPlanId);
        var plan = await _planRepository.GetByIdAsync(request.Model.SubscriptionPlanId, cancellationToken);
        if (plan is null || plan.StatusId != (short)EntityStatusEnum.Active)
        {
            _logger.LogInformation("UpdateStudent: subscription plan {PlanId} missing or inactive", request.Model.SubscriptionPlanId);
            return Result.Failure<StudentDto>(StudentErrors.SubscriptionNotActive);
        }

        _logger.LogInformation("UpdateStudent: applying update to student {StudentId}", student.Id);
        student.ApplyUpdate(request.Model);
        _studentRepository.Update(student);

        _logger.LogInformation("UpdateStudent: writing activity log entry");
        _activityLogRepository.Add(StudentActivityLogMappings.NewLog(
            student.TenantId, student.BranchId, student.Id,
            StudentActivityType.Updated, $"Student {student.FullName} details were updated.",
            request.Model.ModifiedByEmail ?? "system@tkdhub.com", request.Model.ModifiedByName ?? "System"));

        _logger.LogInformation("UpdateStudent: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UpdateStudent: succeeded — student {StudentId} updated", student.Id);
        return Result.Success(student.ToDto());
    }
}
