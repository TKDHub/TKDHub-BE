using Dojo.Application.Dtos.Classes;
using Dojo.Application.Mappings.Classes;
using Dojo.Application.Mappings.Students;
using Dojo.Application.Models.Class;
using Dojo.Domain.Constants;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Classes;

public sealed record RemoveStudentsFromClassCommand(RemoveStudentsFromClassModel Model) : ICommand<ClassDto>;

internal sealed class RemoveStudentsFromClassCommandHandler : ICommandHandler<RemoveStudentsFromClassCommand, ClassDto>
{
    private readonly IClassRepository              _classRepository;
    private readonly IStudentRepository            _studentRepository;
    private readonly IStudentActivityLogRepository _activityLogRepository;
    private readonly IUnitOfWork                   _unitOfWork;
    private readonly ILogger<RemoveStudentsFromClassCommandHandler> _logger;

    public RemoveStudentsFromClassCommandHandler(
        IClassRepository              classRepository,
        IStudentRepository            studentRepository,
        IStudentActivityLogRepository activityLogRepository,
        IUnitOfWork                   unitOfWork,
        ILogger<RemoveStudentsFromClassCommandHandler> logger)
    {
        _classRepository        = classRepository;
        _studentRepository      = studentRepository;
        _activityLogRepository  = activityLogRepository;
        _unitOfWork             = unitOfWork;
        _logger                 = logger;
    }

    public async Task<Result<ClassDto>> Handle(RemoveStudentsFromClassCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;
        _logger.LogInformation("RemoveStudentsFromClass: starting for class {ClassId}, {Count} student(s) requested", model.ClassId, model.StudentIds.Count);

        if (model.StudentIds.Count == 0)
        {
            _logger.LogInformation("RemoveStudentsFromClass: rejected — no student ids provided");
            return Result.Failure<ClassDto>(ClassErrors.NoStudentsProvided);
        }

        var trainingClass = await _classRepository.GetByIdAsync(model.ClassId, cancellationToken);
        if (trainingClass is null)
        {
            _logger.LogInformation("RemoveStudentsFromClass: class {ClassId} not found", model.ClassId);
            return Result.Failure<ClassDto>(ClassErrors.NotFound);
        }

        var studentIds = model.StudentIds.Distinct().ToList();
        var students = await _studentRepository.GetByIdsAsync(studentIds, cancellationToken);

        // Only detach students who actually belong to this class — anything else is a no-op,
        // not an error, since the caller's intent ("get these students off this class") already holds.
        var removed = 0;
        foreach (var student in students.Where(s => s.ClassId == trainingClass.Id))
        {
            _logger.LogInformation("RemoveStudentsFromClass: detaching student {StudentId} from class {ClassId}", student.Id, trainingClass.Id);
            student.ClassId = null;
            _studentRepository.Update(student);
            removed++;

            _activityLogRepository.Add(StudentActivityLogMappings.NewLog(
                student.TenantId, student.BranchId, student.Id,
                StudentActivityType.RemovedFromClass, $"Student {student.FullName} was removed from class {trainingClass.Name}.",
                model.PerformedByEmail, model.PerformedByName));
        }

        _logger.LogInformation("RemoveStudentsFromClass: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await _classRepository.GetByIdWithStudentsAsync(trainingClass.Id, cancellationToken)
            ?? trainingClass;

        _logger.LogInformation("RemoveStudentsFromClass: succeeded — {Removed} student(s) removed from class {ClassId}", removed, trainingClass.Id);
        return Result.Success(refreshed.ToDto(refreshed.Students.ToListDtos()));
    }
}
