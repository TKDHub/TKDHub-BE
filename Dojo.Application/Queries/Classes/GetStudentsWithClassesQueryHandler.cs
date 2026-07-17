using Dojo.Application.Dtos.Classes;
using Dojo.Application.Mappings.Classes;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.Classes;

/// <summary>Report: every student alongside their currently linked class, belt, and age.</summary>
public sealed record GetStudentsWithClassesQuery(PagedRequest Request) : IQuery<PagedResult<StudentClassSummaryDto>>;

internal sealed class GetStudentsWithClassesQueryHandler : IQueryHandler<GetStudentsWithClassesQuery, PagedResult<StudentClassSummaryDto>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IUserContext       _userContext;
    private readonly IBranchContext     _branchContext;
    private readonly ILogger<GetStudentsWithClassesQueryHandler> _logger;

    public GetStudentsWithClassesQueryHandler(
        IStudentRepository studentRepository,
        IUserContext       userContext,
        IBranchContext     branchContext,
        ILogger<GetStudentsWithClassesQueryHandler> logger)
    {
        _studentRepository = studentRepository;
        _userContext       = userContext;
        _branchContext     = branchContext;
        _logger            = logger;
    }

    public async Task<Result<PagedResult<StudentClassSummaryDto>>> Handle(GetStudentsWithClassesQuery request, CancellationToken cancellationToken)
    {
        var branchId = _userContext.IsSuperAdmin ? (Guid?)null : _branchContext.BranchId;
        _logger.LogInformation("GetStudentsWithClasses: querying page {Page} size {PageSize}, branch scope {BranchId}",
            request.Request.Page, request.Request.PageSize, branchId);

        var result = await _studentRepository.GetPagedWithClassAsync(request.Request, branchId, cancellationToken);

        _logger.LogInformation("GetStudentsWithClasses: returned {Count} of {Total} row(s)", result.Items.Count, result.TotalCount);
        return Result.Success(PagedResult<StudentClassSummaryDto>.Create(
            result.Items.ToClassSummaryDtos(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
