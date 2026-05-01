using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;
using Shared.Infrastructure.Extensions;

namespace Identity.Infrastructure.Persistence.Repositories;

internal sealed class BranchRepository : IBranchRepository
{
    private readonly IdentityDbContext _dbContext;

    public BranchRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Branches
            .FirstOrDefaultAsync(b => b.Id == id && b.StatusId == (short)EntityStatusEnum.Active, cancellationToken);

    public async Task<Branch?> GetByIdIgnoringFiltersAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Branches
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id && b.StatusId == (short)EntityStatusEnum.Active, cancellationToken);

    public async Task<List<Branch>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Branches
            .Where(b => b.StatusId == (short)EntityStatusEnum.Active)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Branch>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
        => await _dbContext.Branches
            .Where(b => b.StatusId == (short)EntityStatusEnum.Active)
            .ToPagedResultAsync(request, cancellationToken);

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default)
        => await _dbContext.Branches.AnyAsync(
            b => b.StatusId == (short)EntityStatusEnum.Active
                 && b.Name == name.Trim()
                 && (excludeId == null || b.Id != excludeId.Value),
            cancellationToken);

    public void Add(Branch branch) => _dbContext.Branches.Add(branch);

    public void Update(Branch branch) => _dbContext.Branches.Update(branch);

    public void Remove(Branch branch) => _dbContext.Branches.Remove(branch);
}
