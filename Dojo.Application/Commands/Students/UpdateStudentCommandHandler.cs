using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Application.Models.Student;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Students;

public sealed record UpdateStudentCommand(StudentModel Model) : ICommand<StudentDto>;

internal sealed class UpdateStudentCommandHandler : ICommandHandler<UpdateStudentCommand, StudentDto>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateStudentCommandHandler> _logger;

    public UpdateStudentCommandHandler(IStudentRepository studentRepository, IUnitOfWork unitOfWork, ILogger<UpdateStudentCommandHandler> logger)
    {
        _studentRepository = studentRepository;
        _unitOfWork        = unitOfWork;
        _logger            = logger;
    }

    public async Task<Result<StudentDto>> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Model.FirstName))
            return Result.Failure<StudentDto>(StudentErrors.FirstNameRequired);

        if (string.IsNullOrWhiteSpace(request.Model.LastName))
            return Result.Failure<StudentDto>(StudentErrors.LastNameRequired);

        if (string.IsNullOrWhiteSpace(request.Model.Email))
            return Result.Failure<StudentDto>(StudentErrors.EmailRequired);

        var student = await _studentRepository.GetByIdIgnoringFiltersAsync(request.Model.StudentId!.Value, cancellationToken);
        if (student is null)
            return Result.Failure<StudentDto>(StudentErrors.NotFound);

        var newEmail = request.Model.Email.Trim().ToLowerInvariant();
        if (!string.Equals(student.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await _studentRepository.ExistsByEmailAsync(newEmail, student.Id, cancellationToken);
            if (emailExists)
            {
                _logger.LogWarning("Email '{Email}' already in use — update rejected for student {StudentId}", newEmail, student.Id);
                return Result.Failure<StudentDto>(StudentErrors.EmailAlreadyExists);
            }
        }

        student.ApplyUpdate(request.Model);

        _studentRepository.Update(student);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Student {StudentId} updated successfully", student.Id);

        return Result.Success(student.ToDto());
    }
}
