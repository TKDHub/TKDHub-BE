using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.Students;

public sealed record GetStudentByIdQuery(Guid StudentId) : IQuery<StudentDto>;

internal sealed class GetStudentByIdQueryHandler : IQueryHandler<GetStudentByIdQuery, StudentDto>
{
    private readonly IStudentRepository _studentRepository;

    public GetStudentByIdQueryHandler(IStudentRepository studentRepository)
        => _studentRepository = studentRepository;

    public async Task<Result<StudentDto>> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
        if (student is null)
            return Result.Failure<StudentDto>(StudentErrors.NotFound);

        return Result.Success(student.ToDto());
    }
}
