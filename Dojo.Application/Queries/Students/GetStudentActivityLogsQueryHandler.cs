using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.Students;

public sealed record GetStudentActivityLogsQuery(Guid StudentId, PagedRequest Request) : IQuery<PagedResult<StudentActivityLogDto>>;

internal sealed class GetStudentActivityLogsQueryHandler : IQueryHandler<GetStudentActivityLogsQuery, PagedResult<StudentActivityLogDto>>
{
    private readonly IStudentRepository            _studentRepository;
    private readonly IStudentActivityLogRepository _activityLogRepository;
    private readonly ILogger<GetStudentActivityLogsQueryHandler> _logger;

    public GetStudentActivityLogsQueryHandler(
        IStudentRepository            studentRepository,
        IStudentActivityLogRepository activityLogRepository,
        ILogger<GetStudentActivityLogsQueryHandler> logger)
    {
        _studentRepository      = studentRepository;
        _activityLogRepository  = activityLogRepository;
        _logger                 = logger;
    }

    public async Task<Result<PagedResult<StudentActivityLogDto>>> Handle(GetStudentActivityLogsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetStudentActivityLogs: looking up student {StudentId}", request.StudentId);

        var student = await _studentRepository.GetByIdIncludingDeletedAsync(request.StudentId, cancellationToken);
        if (student is null)
        {
            _logger.LogInformation("GetStudentActivityLogs: student {StudentId} not found", request.StudentId);
            return Result.Failure<PagedResult<StudentActivityLogDto>>(StudentErrors.NotFound);
        }

        var result = await _activityLogRepository.GetPagedByStudentIdAsync(request.StudentId, request.Request, cancellationToken);

        _logger.LogInformation("GetStudentActivityLogs: returned {Count} of {Total} log entr(y/ies) for student {StudentId}",
            result.Items.Count, result.TotalCount, request.StudentId);
        return Result.Success(PagedResult<StudentActivityLogDto>.Create(
            result.Items.ToListDtos(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
