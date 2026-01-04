using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;

namespace Identity.Infrastructure.Persistence.Repositories
{
    internal sealed class TenantRepository : ITenantRepository
    {
        private readonly IdentityDbContext _dbContext;

        public TenantRepository(IdentityDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Tenants.FirstOrDefaultAsync(t => t.StatusId == (short)EntityStatusEnum.Active && t.Id == id, cancellationToken);
        }

        public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Tenants.FirstOrDefaultAsync(t => t.StatusId == (short)EntityStatusEnum.Active && t.Subdomain == subdomain.ToLowerInvariant(), cancellationToken);
        }

        public async Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Tenants.Where(t => t.StatusId == (short)EntityStatusEnum.Active).OrderBy(t => t.Name).ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Tenants.AnyAsync(t => t.Subdomain == subdomain.ToLowerInvariant(), cancellationToken);
        }

        public void Add(Tenant tenant)
        {
            _dbContext.Tenants.Add(tenant);
        }

        public void Update(Tenant tenant)
        {
            _dbContext.Tenants.Update(tenant);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}