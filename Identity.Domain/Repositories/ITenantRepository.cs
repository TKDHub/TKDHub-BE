using Identity.Domain.Entities;
using Shared.Domain.Pagination;

namespace Identity.Domain.Repositories
{
    public interface ITenantRepository
    {
        Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Tenant?> GetByIdIgnoringFiltersAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);
        Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<Tenant>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
        Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);
        void Add(Tenant tenant);
        void Update(Tenant tenant);
        void Remove(Tenant tenant);
    }
}