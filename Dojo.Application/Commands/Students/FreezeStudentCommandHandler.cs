using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Application.Models.Student;
using Dojo.Domain.Constants;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Students;

public sealed record FreezeStudentCommand(FreezeStudentModel Model) : ICommand<StudentDto>;

/// <summary>
/// Pauses a student's membership. The days remaining until their original EndDate are
/// snapshotted so a future unfreeze can resume the clock from exactly where it left off,
/// rather than losing that remaining time.
/// </summary>
internal sealed class FreezeStudentCommandHandler : ICommandHandler<FreezeStudentCommand, StudentDto>
{
    private readonly IStudentRepository            _studentRepository;
    private readonly IStudentActivityLogRepository _activityLogRepository;
    private readonly IUnitOfWork                   _unitOfWork;
    private readonly ILogger<FreezeStudentCommandHandler> _logger;

    public FreezeStudentCommandHandler(
        IStudentRepository            studentRepository,
        IStudentActivityLogRepository activityLogRepository,
        IUnitOfWork                   unitOfWork,
        ILogger<FreezeStudentCommandHandler> logger)
    {
        _studentRepository      = studentRepository;
        _activityLogRepository  = activityLogRepository;
        _unitOfWork             = unitOfWork;
        _logger                 = logger;
    }

    public async Task<Result<StudentDto>> Handle(FreezeStudentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("FreezeStudent: starting for student {StudentId}", request.Model.StudentId);

        var student = await _studentRepository.GetByIdAsync(request.Model.StudentId, cancellationToken);
        if (student is null)
        {
            _logger.LogInformation("FreezeStudent: student {StudentId} not found", request.Model.StudentId);
            return Result.Failure<StudentDto>(StudentErrors.NotFound);
        }

        if (student.StatusId == (short)StudentStatusEnum.Frozen)
        {
            _logger.LogInformation("FreezeStudent: student {StudentId} already frozen", student.Id);
            return Result.Failure<StudentDto>(StudentErrors.AlreadyFrozen);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var remainingDays = Math.Max(0, student.EndDate.DayNumber - today.DayNumber);
        _logger.LogInformation("FreezeStudent: snapshotting {RemainingDays} remaining day(s) for student {StudentId}", remainingDays, student.Id);

        student.RemainingDurationDays = remainingDays;
        student.FrozenOn              = today;
        student.FrozenByEmail         = request.Model.FrozenByEmail;
        student.FrozenByName          = request.Model.FrozenByName;
        student.StatusId              = (short)StudentStatusEnum.Frozen;
        student.ModifiedOn            = DateTimeOffset.UtcNow;
        student.ModifiedByEmail       = request.Model.FrozenByEmail;
        student.ModifiedByName        = request.Model.FrozenByName;

        _studentRepository.Update(student);

        _logger.LogInformation("FreezeStudent: writing activity log entry");
        _activityLogRepository.Add(StudentActivityLogMappings.NewLog(
            student.TenantId, student.BranchId, student.Id,
            StudentActivityType.Frozen, $"Student {student.FullName} was frozen ({remainingDays} day(s) remaining).",
            request.Model.FrozenByEmail, request.Model.FrozenByName));

        _logger.LogInformation("FreezeStudent: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("FreezeStudent: succeeded — student {StudentId} frozen", student.Id);
        return Result.Success(student.ToDto());
    }
}
