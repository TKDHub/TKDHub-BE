using Dojo.Application.Dtos.Students;
using Dojo.Application.Mappings.Students;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.Students;

public sealed record GetAllStudentsQuery(PagedRequest Request) : IQuery<PagedResult<StudentDto>>;

internal sealed class GetAllStudentsQueryHandler : IQueryHandler<GetAllStudentsQuery, PagedResult<StudentDto>>
{
    private readonly IStudentRepository _studentRepository;

    public GetAllStudentsQueryHandler(IStudentRepository studentRepository)
        => _studentRepository = studentRepository;

    public async Task<Result<PagedResult<StudentDto>>> Handle(GetAllStudentsQuery request, CancellationToken cancellationToken)
    {
        var result = await _studentRepository.GetPagedAsync(request.Request, cancellationToken);

        return Result.Success(PagedResult<StudentDto>.Create(
            result.Items.ToListDtos(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
