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

public sealed record ReactivateStudentCommand(ReactivateStudentModel Model) : ICommand<StudentDto>;

/// <summary>
/// Reactivates a Frozen or Inactive (soft-deleted) student. A frozen student resumes the
/// clock from the days snapshotted at freeze time; an inactive student re-registers against
/// a subscription plan, exactly like a fresh CreateStudent.
/// </summary>
internal sealed class ReactivateStudentCommandHandler : ICommandHandler<ReactivateStudentCommand, StudentDto>
{
    private readonly IStudentRepository            _studentRepository;
    private readonly ISubscriptionPlanRepository   _planRepository;
    private readonly IStudentActivityLogRepository _activityLogRepository;
    private readonly IUnitOfWork                   _unitOfWork;
    private readonly ILogger<ReactivateStudentCommandHandler> _logger;

    public ReactivateStudentCommandHandler(
        IStudentRepository            studentRepository,
        ISubscriptionPlanRepository   planRepository,
        IStudentActivityLogRepository activityLogRepository,
        IUnitOfWork                   unitOfWork,
        ILogger<ReactivateStudentCommandHandler> logger)
    {
        _studentRepository      = studentRepository;
        _planRepository         = planRepository;
        _activityLogRepository  = activityLogRepository;
        _unitOfWork             = unitOfWork;
        _logger                 = logger;
    }

    public async Task<Result<StudentDto>> Handle(ReactivateStudentCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;
        _logger.LogInformation("ReactivateStudent: starting for student {StudentId}", model.StudentId);

        var student = await _studentRepository.GetByIdIncludingDeletedAsync(model.StudentId, cancellationToken);
        if (student is null)
        {
            _logger.LogInformation("ReactivateStudent: student {StudentId} not found", model.StudentId);
            return Result.Failure<StudentDto>(StudentErrors.NotFound);
        }

        _logger.LogInformation("ReactivateStudent: student {StudentId} is currently {Status}", student.Id, (StudentStatusEnum)student.StatusId);

        switch ((StudentStatusEnum)student.StatusId)
        {
            case StudentStatusEnum.Active:
                _logger.LogInformation("ReactivateStudent: student {StudentId} already active", student.Id);
                return Result.Failure<StudentDto>(StudentErrors.AlreadyActive);

            case StudentStatusEnum.Frozen:
                // Resume the paused clock: the days left at freeze time now count forward
                // from the new start date.
                _logger.LogInformation("ReactivateStudent: resuming frozen clock for student {StudentId} with {RemainingDays} day(s) left",
                    student.Id, student.RemainingDurationDays ?? 0);
                student.StartDate = model.StartDate;
                student.EndDate   = model.StartDate.AddDays(student.RemainingDurationDays ?? 0);
                break;

            default: // Inactive (soft-deleted) — re-register like a fresh CreateStudent
                var planId = model.SubscriptionPlanId ?? student.SubscriptionPlanId;
                _logger.LogInformation("ReactivateStudent: re-registering inactive student {StudentId} against plan {PlanId}", student.Id, planId);
                var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
                if (plan is null)
                {
                    _logger.LogInformation("ReactivateStudent: plan {PlanId} not found", planId);
                    return Result.Failure<StudentDto>(StudentErrors.NoActivePlans);
                }

                if (plan.StatusId != (short)EntityStatusEnum.Active)
                {
                    _logger.LogInformation("ReactivateStudent: plan {PlanId} is not active", plan.Id);
                    return Result.Failure<StudentDto>(StudentErrors.SubscriptionNotActive);
                }

                student.SubscriptionPlanId = plan.Id;
                student.Price              = plan.Price;
                student.DurationMonths     = plan.DurationMonths;
                student.StartDate          = model.StartDate;
                student.EndDate            = model.StartDate.AddMonths(plan.DurationMonths);
                break;
        }

        student.StatusId              = (short)StudentStatusEnum.Active;
        student.FrozenOn              = null;
        student.FrozenByEmail         = null;
        student.FrozenByName          = null;
        student.RemainingDurationDays = null;
        student.ModifiedOn            = DateTimeOffset.UtcNow;
        student.ModifiedByEmail       = model.ModifiedByEmail;
        student.ModifiedByName        = model.ModifiedByName;

        _studentRepository.Update(student);

        _logger.LogInformation("ReactivateStudent: writing activity log entry");
        _activityLogRepository.Add(StudentActivityLogMappings.NewLog(
            student.TenantId, student.BranchId, student.Id,
            StudentActivityType.Reactivated, $"Student {student.FullName} was reactivated.",
            model.ModifiedByEmail, model.ModifiedByName));

        _logger.LogInformation("ReactivateStudent: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("ReactivateStudent: succeeded — student {StudentId} reactivated", student.Id);
        return Result.Success(student.ToDto());
    }
}
