using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Students;

public sealed record DeleteStudentCommand(Guid StudentId) : ICommand;

internal sealed class DeleteStudentCommandHandler : ICommandHandler<DeleteStudentCommand>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteStudentCommandHandler(IStudentRepository studentRepository, IUnitOfWork unitOfWork)
    {
        _studentRepository = studentRepository;
        _unitOfWork        = unitOfWork;
    }

    public async Task<Result> Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
        if (student is null)
            return Result.Failure(StudentErrors.NotFound);

        _studentRepository.Remove(student);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
