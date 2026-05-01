using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Pagination;
using Shared.Domain.Enums;
using Shared.Infrastructure.Extensions;

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
            return await _dbContext.Tenants
                .Include(t => t.Branches.Where(b => b.StatusId == (short)EntityStatusEnum.Active))
                .FirstOrDefaultAsync(t => t.StatusId == (short)EntityStatusEnum.Active && t.Id == id, cancellationToken);
        }

        public async Task<Tenant?> GetByIdIgnoringFiltersAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Tenants
                .IgnoreQueryFilters()
                .Include(t => t.Branches.Where(b => b.StatusId == (short)EntityStatusEnum.Active))
                .FirstOrDefaultAsync(t => t.StatusId == (short)EntityStatusEnum.Active && t.Id == id, cancellationToken);
        }

        public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Tenants
                .Include(t => t.Branches.Where(b => b.StatusId == (short)EntityStatusEnum.Active))
                .FirstOrDefaultAsync(t => t.StatusId == (short)EntityStatusEnum.Active && t.Subdomain == subdomain.ToLowerInvariant(), cancellationToken);
        }

        public async Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Tenants.Where(t => t.StatusId == (short)EntityStatusEnum.Active).OrderBy(t => t.Name).ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<Tenant>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Tenants
                .Where(t => t.StatusId == (short)EntityStatusEnum.Active)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Tenants.AnyAsync(
                t => t.StatusId == (short)EntityStatusEnum.Active && t.Subdomain == subdomain.ToLowerInvariant(),
                cancellationToken);
        }

        public void Add(Tenant tenant)
        {
            _dbContext.Tenants.Add(tenant);
        }

        public void Update(Tenant tenant)
        {
            _dbContext.Tenants.Update(tenant);
        }

        public void Remove(Tenant tenant)
        {
            _dbContext.Tenants.Update(tenant);
        }
    }
}
