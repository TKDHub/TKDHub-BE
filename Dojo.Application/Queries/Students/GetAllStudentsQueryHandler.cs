using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Domain.Repositories;
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

    public GetAllStudentsQueryHandler(
        IStudentRepository studentRepository,
        IUserContext userContext,
        IBranchContext branchContext)
    {
        _studentRepository = studentRepository;
        _userContext       = userContext;
        _branchContext     = branchContext;
    }

    public async Task<Result<PagedResult<StudentDto>>> Handle(GetAllStudentsQuery request, CancellationToken cancellationToken)
    {
        var branchId = _userContext.IsSuperAdmin ? (Guid?)null : _branchContext.BranchId;

        var result = await _studentRepository.GetPagedAsync(request.Request, branchId, cancellationToken);

        return Result.Success(PagedResult<StudentDto>.Create(
            result.Items.ToListDtos(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
