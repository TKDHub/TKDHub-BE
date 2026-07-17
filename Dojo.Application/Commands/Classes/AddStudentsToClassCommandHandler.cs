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

public sealed record AddStudentsToClassCommand(AddStudentsToClassModel Model) : ICommand<ClassDto>;

/// <summary>
/// Enrolls students into a class. Since a student belongs to at most one class at a time,
/// any student already in another class is silently moved rather than rejected.
/// </summary>
internal sealed class AddStudentsToClassCommandHandler : ICommandHandler<AddStudentsToClassCommand, ClassDto>
{
    private readonly IClassRepository              _classRepository;
    private readonly IStudentRepository            _studentRepository;
    private readonly IStudentActivityLogRepository _activityLogRepository;
    private readonly IUnitOfWork                   _unitOfWork;
    private readonly ILogger<AddStudentsToClassCommandHandler> _logger;

    public AddStudentsToClassCommandHandler(
        IClassRepository              classRepository,
        IStudentRepository            studentRepository,
        IStudentActivityLogRepository activityLogRepository,
        IUnitOfWork                   unitOfWork,
        ILogger<AddStudentsToClassCommandHandler> logger)
    {
        _classRepository        = classRepository;
        _studentRepository      = studentRepository;
        _activityLogRepository  = activityLogRepository;
        _unitOfWork             = unitOfWork;
        _logger                 = logger;
    }

    public async Task<Result<ClassDto>> Handle(AddStudentsToClassCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;
        _logger.LogInformation("AddStudentsToClass: starting for class {ClassId}, {Count} student(s) requested", model.ClassId, model.StudentIds.Count);

        if (model.StudentIds.Count == 0)
        {
            _logger.LogInformation("AddStudentsToClass: rejected — no student ids provided");
            return Result.Failure<ClassDto>(ClassErrors.NoStudentsProvided);
        }

        var trainingClass = await _classRepository.GetByIdAsync(model.ClassId, cancellationToken);
        if (trainingClass is null)
        {
            _logger.LogInformation("AddStudentsToClass: class {ClassId} not found", model.ClassId);
            return Result.Failure<ClassDto>(ClassErrors.NotFound);
        }

        var studentIds = model.StudentIds.Distinct().ToList();
        var students = await _studentRepository.GetByIdsAsync(studentIds, cancellationToken);
        if (students.Count != studentIds.Count)
        {
            _logger.LogInformation("AddStudentsToClass: {Found} of {Requested} student ids resolved — some not found", students.Count, studentIds.Count);
            return Result.Failure<ClassDto>(ClassErrors.StudentNotFound);
        }

        foreach (var student in students)
        {
            _logger.LogInformation("AddStudentsToClass: enrolling student {StudentId} into class {ClassId}", student.Id, trainingClass.Id);
            student.ClassId = trainingClass.Id;
            _studentRepository.Update(student);

            _activityLogRepository.Add(StudentActivityLogMappings.NewLog(
                student.TenantId, student.BranchId, student.Id,
                StudentActivityType.AddedToClass, $"Student {student.FullName} was added to class {trainingClass.Name}.",
                model.PerformedByEmail, model.PerformedByName));
        }

        _logger.LogInformation("AddStudentsToClass: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await _classRepository.GetByIdWithStudentsAsync(trainingClass.Id, cancellationToken)
            ?? trainingClass;

        _logger.LogInformation("AddStudentsToClass: succeeded — {Count} student(s) enrolled in class {ClassId}", students.Count, trainingClass.Id);
        return Result.Success(refreshed.ToDto(refreshed.Students.ToListDtos()));
    }
}
