using Identity.Domain.Entities;
using Shared.Domain.Pagination;

namespace Identity.Domain.Repositories;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Branch?> GetByIdIgnoringFiltersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Branch>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<Branch>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default);
    void Add(Branch branch);
    void Update(Branch branch);
    void Remove(Branch branch);
}
