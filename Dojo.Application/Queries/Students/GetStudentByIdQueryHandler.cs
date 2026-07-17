using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.Students;

public sealed record GetStudentByIdQuery(Guid StudentId) : IQuery<StudentDto>;

internal sealed class GetStudentByIdQueryHandler : IQueryHandler<GetStudentByIdQuery, StudentDto>
{
    private readonly IStudentRepository _studentRepository;
    private readonly ILogger<GetStudentByIdQueryHandler> _logger;

    public GetStudentByIdQueryHandler(IStudentRepository studentRepository, ILogger<GetStudentByIdQueryHandler> logger)
    {
        _studentRepository = studentRepository;
        _logger             = logger;
    }

    public async Task<Result<StudentDto>> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetStudentById: looking up student {StudentId}", request.StudentId);

        var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
        if (student is null)
        {
            _logger.LogInformation("GetStudentById: student {StudentId} not found", request.StudentId);
            return Result.Failure<StudentDto>(StudentErrors.NotFound);
        }

        _logger.LogInformation("GetStudentById: found student {StudentId}", student.Id);
        return Result.Success(student.ToDto());
    }
}
