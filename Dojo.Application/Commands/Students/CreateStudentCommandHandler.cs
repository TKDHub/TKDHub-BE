using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Application.Models.Student;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Students;

public sealed record CreateStudentCommand(StudentModel Model) : ICommand<StudentDto>;

internal sealed class CreateStudentCommandHandler : ICommandHandler<CreateStudentCommand, StudentDto>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStudentCommandHandler(IStudentRepository studentRepository, IUnitOfWork unitOfWork)
    {
        _studentRepository = studentRepository;
        _unitOfWork        = unitOfWork;
    }

    public async Task<Result<StudentDto>> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Model.FirstName))
            return Result.Failure<StudentDto>(StudentErrors.FirstNameRequired);

        if (string.IsNullOrWhiteSpace(request.Model.LastName))
            return Result.Failure<StudentDto>(StudentErrors.LastNameRequired);

        if (string.IsNullOrWhiteSpace(request.Model.Email))
            return Result.Failure<StudentDto>(StudentErrors.EmailRequired);

        if (request.Model.BranchId == Guid.Empty)
            return Result.Failure<StudentDto>(StudentErrors.BranchRequired);

        var emailExists = await _studentRepository.ExistsByEmailAsync(request.Model.Email, null, cancellationToken);
        if (emailExists)
            return Result.Failure<StudentDto>(StudentErrors.EmailAlreadyExists);

        var student = request.Model.ToEntity();

        _studentRepository.Add(student);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(student.ToDto());
    }
}
