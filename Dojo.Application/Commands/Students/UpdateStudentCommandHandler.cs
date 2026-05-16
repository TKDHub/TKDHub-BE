using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Application.Models.Student;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Students;

public sealed record UpdateStudentCommand(StudentModel Model) : ICommand<StudentDto>;

internal sealed class UpdateStudentCommandHandler : ICommandHandler<UpdateStudentCommand, StudentDto>
{
    private readonly IStudentRepository          _studentRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IUnitOfWork                 _unitOfWork;

    public UpdateStudentCommandHandler(
        IStudentRepository          studentRepository,
        ISubscriptionPlanRepository planRepository,
        IUnitOfWork                 unitOfWork)
    {
        _studentRepository = studentRepository;
        _planRepository    = planRepository;
        _unitOfWork        = unitOfWork;
    }

    public async Task<Result<StudentDto>> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Model.FirstName))
            return Result.Failure<StudentDto>(StudentErrors.FirstNameRequired);

        if (string.IsNullOrWhiteSpace(request.Model.LastName))
            return Result.Failure<StudentDto>(StudentErrors.LastNameRequired);

        if (string.IsNullOrWhiteSpace(request.Model.PhoneNumber))
            return Result.Failure<StudentDto>(StudentErrors.PhoneRequired);

        var student = await _studentRepository.GetByIdIgnoringFiltersAsync(
            request.Model.StudentId!.Value, cancellationToken);

        if (student is null)
            return Result.Failure<StudentDto>(StudentErrors.NotFound);

        if (!string.IsNullOrWhiteSpace(request.Model.Email))
        {
            var newEmail = request.Model.Email.Trim().ToLowerInvariant();
            if (!string.Equals(student.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await _studentRepository.ExistsByEmailAsync(newEmail, student.Id, cancellationToken);
                if (emailExists)
                    return Result.Failure<StudentDto>(StudentErrors.EmailAlreadyExists);
            }
        }

        if (request.Model.SubscriptionPlanId == Guid.Empty)
            return Result.Failure<StudentDto>(StudentErrors.SubscriptionRequired);

        var plan = await _planRepository.GetByIdAsync(request.Model.SubscriptionPlanId, cancellationToken);
        if (plan is null || plan.StatusId != (short)EntityStatusEnum.Active)
            return Result.Failure<StudentDto>(StudentErrors.SubscriptionNotActive);

        student.ApplyUpdate(request.Model);
        _studentRepository.Update(student);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(student.ToDto());
    }
}
