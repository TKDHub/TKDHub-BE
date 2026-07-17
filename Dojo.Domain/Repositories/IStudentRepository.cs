using Dojo.Domain.Entities;
using Shared.Domain.Pagination;

namespace Dojo.Domain.Repositories;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Student?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Student?> GetByIdIgnoringFiltersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Student>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Student>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<PagedResult<Student>> GetPagedAsync(PagedRequest request, Guid? branchId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<Student>> GetPagedWithClassAsync(PagedRequest request, Guid? branchId = null, CancellationToken cancellationToken = default);

    /// <summary>Active students whose EndDate has passed, across ALL tenants/branches — for the daily expiry sweep.</summary>
    Task<List<Student>> GetExpiredActiveStudentsAsync(DateOnly asOf, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, Guid? excludeId, CancellationToken cancellationToken = default);
    void Add(Student student);
    void Update(Student student);
    void Remove(Student student);
}
