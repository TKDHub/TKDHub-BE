using Dojo.Domain.Entities;
using Shared.Domain.Pagination;

namespace Dojo.Domain.Repositories;

public interface IStudentActivityLogRepository
{
    void Add(StudentActivityLog log);
    Task<PagedResult<StudentActivityLog>> GetPagedByStudentIdAsync(Guid studentId, PagedRequest request, CancellationToken cancellationToken = default);
}
