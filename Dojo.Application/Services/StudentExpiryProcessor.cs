using Dojo.Application.Mappings.Students;
using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Domain.Repositories;

namespace Dojo.Application.Services;

/// <summary>
/// Core logic for the daily student-expiry sweep. Kept separate from the hosted-service timer
/// loop so it can be unit tested like any other Application-layer piece.
/// </summary>
public sealed class StudentExpiryProcessor : IStudentExpiryProcessor
{
    private const string SystemEmail = "system@tkdhub.com";
    private const string SystemName  = "System";

    private readonly IStudentRepository             _studentRepository;
    private readonly IStudentActivityLogRepository  _activityLogRepository;
    private readonly IUnitOfWork                    _unitOfWork;
    private readonly INotificationTargetsService    _notificationTargets;
    private readonly IWhatsAppService                _whatsApp;
    private readonly ILogger<StudentExpiryProcessor> _logger;

    public StudentExpiryProcessor(
        IStudentRepository             studentRepository,
        IStudentActivityLogRepository  activityLogRepository,
        IUnitOfWork                    unitOfWork,
        INotificationTargetsService    notificationTargets,
        IWhatsAppService                whatsApp,
        ILogger<StudentExpiryProcessor> logger)
    {
        _studentRepository      = studentRepository;
        _activityLogRepository  = activityLogRepository;
        _unitOfWork             = unitOfWork;
        _notificationTargets    = notificationTargets;
        _whatsApp               = whatsApp;
        _logger                 = logger;
    }

    public async Task<int> ProcessExpiredStudentsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expired = await _studentRepository.GetExpiredActiveStudentsAsync(today, cancellationToken);

        var processed = 0;
        foreach (var student in expired)
        {
            try
            {
                // Persist first, then notify — a WhatsApp failure must never look like the
                // student was deactivated when the DB write didn't actually happen, and one
                // student's failure must not roll back or block the rest of the batch.
                student.StatusId = (short)StudentStatusEnum.Inactive;
                _studentRepository.Update(student);

                _activityLogRepository.Add(StudentActivityLogMappings.NewLog(
                    student.TenantId, student.BranchId, student.Id,
                    StudentActivityType.ExpiredAutomatically,
                    $"Student {student.FullName}'s membership expired on {student.EndDate:yyyy-MM-dd} and was automatically marked inactive.",
                    SystemEmail, SystemName));

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await NotifyAsync(student, cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process expired student {StudentId}", student.Id);
            }
        }

        return processed;
    }

    private async Task NotifyAsync(Student student, CancellationToken cancellationToken)
    {
        var studentMessage =
            $"Hi {student.FullName}, your membership ended on {student.EndDate:yyyy-MM-dd} and your account is now inactive. " +
            "Please contact your branch to renew.";

        if (!string.IsNullOrWhiteSpace(student.PhoneNumber))
            await _whatsApp.SendMessageAsync(student.PhoneNumber, studentMessage, cancellationToken);

        var targets = await _notificationTargets.GetAdminsAndSuperAdminsAsync(student.TenantId, student.BranchId, cancellationToken);

        var adminMessage = $"{student.FullName}'s membership ended on {student.EndDate:yyyy-MM-dd} and has been marked inactive.";

        foreach (var target in targets)
        {
            if (!string.IsNullOrWhiteSpace(target.PhoneNumber))
                await _whatsApp.SendMessageAsync(target.PhoneNumber!, adminMessage, cancellationToken);
        }
    }
}
