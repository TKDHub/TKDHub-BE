using Dojo.Application.Dtos.Classes;
using Dojo.Application.Mappings.Classes;
using Dojo.Application.Mappings.Students;
using Dojo.Application.Models.Class;
using Dojo.Domain.Constants;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Classes;

public sealed record CreateClassCommand(ClassModel Model, Guid BranchId, Guid TenantId) : ICommand<ClassDto>;

internal sealed class CreateClassCommandHandler : ICommandHandler<CreateClassCommand, ClassDto>
{
    private readonly IClassRepository              _classRepository;
    private readonly IStudentRepository            _studentRepository;
    private readonly IBranchService                _branchService;
    private readonly IStudentActivityLogRepository _activityLogRepository;
    private readonly IUnitOfWork                   _unitOfWork;
    private readonly ILogger<CreateClassCommandHandler> _logger;

    public CreateClassCommandHandler(
        IClassRepository              classRepository,
        IStudentRepository            studentRepository,
        IBranchService                branchService,
        IStudentActivityLogRepository activityLogRepository,
        IUnitOfWork                   unitOfWork,
        ILogger<CreateClassCommandHandler> logger)
    {
        _classRepository        = classRepository;
        _studentRepository      = studentRepository;
        _branchService          = branchService;
        _activityLogRepository  = activityLogRepository;
        _unitOfWork             = unitOfWork;
        _logger                 = logger;
    }

    public async Task<Result<ClassDto>> Handle(CreateClassCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateClass: starting for branch {BranchId}, tenant {TenantId}", request.BranchId, request.TenantId);

        if (request.BranchId == Guid.Empty)
        {
            _logger.LogInformation("CreateClass: rejected — branch id was empty");
            return Result.Failure<ClassDto>(ClassErrors.BranchRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Model.Name))
        {
            _logger.LogInformation("CreateClass: rejected — name missing");
            return Result.Failure<ClassDto>(ClassErrors.NameRequired);
        }

        if (request.Model.EndTime <= request.Model.StartTime)
        {
            _logger.LogInformation("CreateClass: rejected — end time not after start time");
            return Result.Failure<ClassDto>(ClassErrors.InvalidTimeRange);
        }

        if (request.Model.Weekdays.Count == 0)
        {
            _logger.LogInformation("CreateClass: rejected — no weekdays provided");
            return Result.Failure<ClassDto>(ClassErrors.WeekdaysRequired);
        }

        var branch = await _branchService.GetBranchAsync(request.BranchId, cancellationToken);
        if (branch is null)
        {
            _logger.LogInformation("CreateClass: branch {BranchId} not found", request.BranchId);
            return Result.Failure<ClassDto>(ClassErrors.BranchNotFound);
        }

        if (branch.TenantId != request.TenantId)
        {
            _logger.LogInformation("CreateClass: branch {BranchId} tenant mismatch", request.BranchId);
            return Result.Failure<ClassDto>(ClassErrors.TenantBranchMismatch);
        }

        var nameExists = await _classRepository.ExistsByNameAsync(
            request.Model.Name, request.BranchId, null, cancellationToken);

        if (nameExists)
        {
            _logger.LogInformation("CreateClass: rejected — name {Name} already exists in branch {BranchId}", request.Model.Name, request.BranchId);
            return Result.Failure<ClassDto>(ClassErrors.NameAlreadyExists);
        }

        var trainingClass = request.Model.ToEntity(request.BranchId, request.TenantId);
        _logger.LogInformation("CreateClass: adding class entity");
        _classRepository.Add(trainingClass);

        var studentIds = request.Model.StudentIds.Distinct().ToList();
        if (studentIds.Count > 0)
        {
            _logger.LogInformation("CreateClass: assigning {Count} initial student(s) to new class", studentIds.Count);
            var students = await _studentRepository.GetByIdsAsync(studentIds, cancellationToken);
            if (students.Count != studentIds.Count)
            {
                _logger.LogInformation("CreateClass: {Found} of {Requested} initial student ids resolved — some not found", students.Count, studentIds.Count);
                return Result.Failure<ClassDto>(ClassErrors.StudentNotFound);
            }

            // A student belongs to at most one class — this silently moves them off whatever
            // class they were already in.
            foreach (var student in students)
            {
                student.ClassId = trainingClass.Id;
                _studentRepository.Update(student);

                _activityLogRepository.Add(StudentActivityLogMappings.NewLog(
                    student.TenantId, student.BranchId, student.Id,
                    StudentActivityType.AddedToClass, $"Student {student.FullName} was added to class {trainingClass.Name}.",
                    request.Model.CreatedByEmail, request.Model.CreatedByName));
            }
        }

        _logger.LogInformation("CreateClass: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CreateClass: succeeded — class {ClassId} created", trainingClass.Id);
        return Result.Success(trainingClass.ToDto());
    }
}
