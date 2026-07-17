using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.Students;

public sealed record GetAllStudentsQuery(PagedRequest Request) : IQuery<PagedResult<StudentDto>>;

internal sealed class GetAllStudentsQueryHandler : IQueryHandler<GetAllStudentsQuery, PagedResult<StudentDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IUserContext       _userContext;
    private readonly IBranchContext     _branchContext;
    private readonly ILogger<GetAllStudentsQueryHandler> _logger;

    public GetAllStudentsQueryHandler(
        IStudentRepository studentRepository,
        IUserContext userContext,
        IBranchContext branchContext,
        ILogger<GetAllStudentsQueryHandler> logger)
    {
        _studentRepository = studentRepository;
        _userContext       = userContext;
        _branchContext     = branchContext;
        _logger            = logger;
    }

    public async Task<Result<PagedResult<StudentDto>>> Handle(GetAllStudentsQuery request, CancellationToken cancellationToken)
    {
        var branchId = _userContext.IsSuperAdmin ? (Guid?)null : _branchContext.BranchId;
        _logger.LogInformation("GetAllStudents: querying page {Page} size {PageSize}, branch scope {BranchId}",
            request.Request.Page, request.Request.PageSize, branchId);

        var result = await _studentRepository.GetPagedAsync(request.Request, branchId, cancellationToken);

        _logger.LogInformation("GetAllStudents: returned {Count} of {Total} student(s)", result.Items.Count, result.TotalCount);
        return Result.Success(PagedResult<StudentDto>.Create(
            result.Items.ToListDtos(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
