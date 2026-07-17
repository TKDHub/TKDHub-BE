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

public sealed record MoveStudentsToClassCommand(MoveStudentsToClassModel Model) : ICommand<ClassDto>;

/// <summary>Moves students from one class to another. Returns the target class with its refreshed roster.</summary>
internal sealed class MoveStudentsToClassCommandHandler : ICommandHandler<MoveStudentsToClassCommand, ClassDto>
{
    private readonly IClassRepository              _classRepository;
    private readonly IStudentRepository            _studentRepository;
    private readonly IStudentActivityLogRepository _activityLogRepository;
    private readonly IUnitOfWork                   _unitOfWork;
    private readonly ILogger<MoveStudentsToClassCommandHandler> _logger;

    public MoveStudentsToClassCommandHandler(
        IClassRepository              classRepository,
        IStudentRepository            studentRepository,
        IStudentActivityLogRepository activityLogRepository,
        IUnitOfWork                   unitOfWork,
        ILogger<MoveStudentsToClassCommandHandler> logger)
    {
        _classRepository        = classRepository;
        _studentRepository      = studentRepository;
        _activityLogRepository  = activityLogRepository;
        _unitOfWork             = unitOfWork;
        _logger                 = logger;
    }

    public async Task<Result<ClassDto>> Handle(MoveStudentsToClassCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;
        _logger.LogInformation("MoveStudentsToClass: starting — {Count} student(s) from class {FromClassId} to {ToClassId}",
            model.StudentIds.Count, model.FromClassId, model.ToClassId);

        if (model.StudentIds.Count == 0)
        {
            _logger.LogInformation("MoveStudentsToClass: rejected — no student ids provided");
            return Result.Failure<ClassDto>(ClassErrors.NoStudentsProvided);
        }

        if (model.FromClassId == model.ToClassId)
        {
            _logger.LogInformation("MoveStudentsToClass: rejected — source and target class are the same");
            return Result.Failure<ClassDto>(ClassErrors.SameClass);
        }

        var sourceClass = await _classRepository.GetByIdAsync(model.FromClassId, cancellationToken);
        if (sourceClass is null)
        {
            _logger.LogInformation("MoveStudentsToClass: source class {ClassId} not found", model.FromClassId);
            return Result.Failure<ClassDto>(ClassErrors.NotFound);
        }

        var targetClass = await _classRepository.GetByIdAsync(model.ToClassId, cancellationToken);
        if (targetClass is null)
        {
            _logger.LogInformation("MoveStudentsToClass: target class {ClassId} not found", model.ToClassId);
            return Result.Failure<ClassDto>(ClassErrors.TargetClassNotFound);
        }

        var studentIds = model.StudentIds.Distinct().ToList();
        var students = await _studentRepository.GetByIdsAsync(studentIds, cancellationToken);

        var moved = 0;
        foreach (var student in students.Where(s => s.ClassId == sourceClass.Id))
        {
            _logger.LogInformation("MoveStudentsToClass: moving student {StudentId} from {FromClassId} to {ToClassId}",
                student.Id, sourceClass.Id, targetClass.Id);
            student.ClassId = targetClass.Id;
            _studentRepository.Update(student);
            moved++;

            _activityLogRepository.Add(StudentActivityLogMappings.NewLog(
                student.TenantId, student.BranchId, student.Id,
                StudentActivityType.MovedClass,
                $"Student {student.FullName} was moved from class {sourceClass.Name} to {targetClass.Name}.",
                model.PerformedByEmail, model.PerformedByName));
        }

        _logger.LogInformation("MoveStudentsToClass: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await _classRepository.GetByIdWithStudentsAsync(targetClass.Id, cancellationToken)
            ?? targetClass;

        _logger.LogInformation("MoveStudentsToClass: succeeded — {Moved} student(s) moved into class {ClassId}", moved, targetClass.Id);
        return Result.Success(refreshed.ToDto(refreshed.Students.ToListDtos()));
    }
}
