using Dojo.Application.Dtos.Classes;
using Dojo.Application.Mappings.Classes;
using Dojo.Application.Mappings.Students;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.Classes;

public sealed record GetClassByIdQuery(Guid ClassId) : IQuery<ClassDto>;

internal sealed class GetClassByIdQueryHandler : IQueryHandler<GetClassByIdQuery, ClassDto>
{
    private readonly IClassRepository _classRepository;
    private readonly ILogger<GetClassByIdQueryHandler> _logger;

    public GetClassByIdQueryHandler(IClassRepository classRepository, ILogger<GetClassByIdQueryHandler> logger)
    {
        _classRepository = classRepository;
        _logger          = logger;
    }

    public async Task<Result<ClassDto>> Handle(GetClassByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetClassById: looking up class {ClassId}", request.ClassId);

        var trainingClass = await _classRepository.GetByIdWithStudentsAsync(request.ClassId, cancellationToken);
        if (trainingClass is null)
        {
            _logger.LogInformation("GetClassById: class {ClassId} not found", request.ClassId);
            return Result.Failure<ClassDto>(ClassErrors.NotFound);
        }

        _logger.LogInformation("GetClassById: found class {ClassId} with {StudentCount} student(s)", trainingClass.Id, trainingClass.Students.Count);
        return Result.Success(trainingClass.ToDto(trainingClass.Students.ToListDtos()));
    }
}
