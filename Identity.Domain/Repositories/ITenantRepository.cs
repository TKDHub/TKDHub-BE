using Identity.Domain.Entities;

namespace Identity.Domain.Repositories
{
    public interface ITenantRepository
    {
        Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);
        Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);
        void Add(Tenant tenant);
        void Update(Tenant tenant);
    }
}