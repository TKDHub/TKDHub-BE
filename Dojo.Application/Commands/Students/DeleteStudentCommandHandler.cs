using Dojo.Application.Mappings.Students;
using Dojo.Domain.Constants;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Students;

public sealed record DeleteStudentCommand(Guid StudentId, string DeletedByEmail, string DeletedByName) : ICommand;

internal sealed class DeleteStudentCommandHandler : ICommandHandler<DeleteStudentCommand>
{
    private readonly IStudentRepository            _studentRepository;
    private readonly IStudentActivityLogRepository _activityLogRepository;
    private readonly IUnitOfWork                   _unitOfWork;
    private readonly ILogger<DeleteStudentCommandHandler> _logger;

    public DeleteStudentCommandHandler(
        IStudentRepository            studentRepository,
        IStudentActivityLogRepository activityLogRepository,
        IUnitOfWork                   unitOfWork,
        ILogger<DeleteStudentCommandHandler> logger)
    {
        _studentRepository      = studentRepository;
        _activityLogRepository  = activityLogRepository;
        _unitOfWork             = unitOfWork;
        _logger                 = logger;
    }

    public async Task<Result> Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteStudent: starting for student {StudentId}", request.StudentId);

        var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
        if (student is null)
        {
            _logger.LogInformation("DeleteStudent: student {StudentId} not found", request.StudentId);
            return Result.Failure(StudentErrors.NotFound);
        }

        _logger.LogInformation("DeleteStudent: soft-deleting student {StudentId}", student.Id);
        _studentRepository.Remove(student);

        _logger.LogInformation("DeleteStudent: writing activity log entry");
        _activityLogRepository.Add(StudentActivityLogMappings.NewLog(
            student.TenantId, student.BranchId, student.Id,
            StudentActivityType.Deleted, $"Student {student.FullName} was deleted.",
            request.DeletedByEmail, request.DeletedByName));

        _logger.LogInformation("DeleteStudent: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DeleteStudent: succeeded — student {StudentId} deleted", student.Id);
        return Result.Success();
    }
}
