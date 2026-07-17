using Dojo.Domain.Entities;
using Shared.Domain.Pagination;

namespace Dojo.Domain.Repositories;

public interface IClassRepository
{
    Task<Class?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Class?> GetByIdWithStudentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Class>> GetPagedAsync(PagedRequest request, Guid? branchId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid branchId, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveStudentsAsync(Guid classId, CancellationToken cancellationToken = default);
    void Add(Class trainingClass);
    void Update(Class trainingClass);
}
