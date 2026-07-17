using Dojo.Application.Dtos.Classes;
using Dojo.Application.Mappings.Classes;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.Classes;

public sealed record GetAllClassesQuery(PagedRequest Request) : IQuery<PagedResult<ClassDto>>;

internal sealed class GetAllClassesQueryHandler : IQueryHandler<GetAllClassesQuery, PagedResult<ClassDto>>
{
    private readonly IClassRepository _classRepository;
    private readonly IUserContext     _userContext;
    private readonly IBranchContext   _branchContext;
    private readonly ILogger<GetAllClassesQueryHandler> _logger;

    public GetAllClassesQueryHandler(
        IClassRepository classRepository,
        IUserContext     userContext,
        IBranchContext   branchContext,
        ILogger<GetAllClassesQueryHandler> logger)
    {
        _classRepository = classRepository;
        _userContext     = userContext;
        _branchContext   = branchContext;
        _logger          = logger;
    }

    public async Task<Result<PagedResult<ClassDto>>> Handle(GetAllClassesQuery request, CancellationToken cancellationToken)
    {
        var branchId = _userContext.IsSuperAdmin ? (Guid?)null : _branchContext.BranchId;
        _logger.LogInformation("GetAllClasses: querying page {Page} size {PageSize}, branch scope {BranchId}",
            request.Request.Page, request.Request.PageSize, branchId);

        var result = await _classRepository.GetPagedAsync(request.Request, branchId, cancellationToken);

        _logger.LogInformation("GetAllClasses: returned {Count} of {Total} class(es)", result.Items.Count, result.TotalCount);
        return Result.Success(PagedResult<ClassDto>.Create(
            result.Items.ToListDtos(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
