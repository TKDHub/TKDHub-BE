using Dojo.Domain.Entities;
using Dojo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;
using Shared.Infrastructure.Extensions;
using Dojo.Domain.Enums;

namespace Dojo.Infrastructure.Persistence.Repositories;

internal sealed class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly DojoDbContext _dbContext;

    public SubscriptionPlanRepository(DojoDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(
                p => p.Id == id && p.StatusId != (short)EntityStatusEnum.Deleted,
                cancellationToken);

    public async Task<SubscriptionPlan?> GetByIdWithStudentsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.SubscriptionPlans
            .Include(p => p.Students.Where(s => s.StatusId == (short)StudentStatusEnum.Active)
                                    .OrderBy(s => s.LastName).ThenBy(s => s.FirstName))
            .FirstOrDefaultAsync(
                p => p.Id == id && p.StatusId != (short)EntityStatusEnum.Deleted,
                cancellationToken);

    public async Task<PagedResult<SubscriptionPlan>> GetPagedAsync(
        PagedRequest request,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SubscriptionPlans
            .Where(p => p.StatusId != (short)EntityStatusEnum.Deleted);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<EntityStatusEnum>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(p => p.StatusId == (short)parsed);
        }

        return await query
            .OrderBy(p => p.Name)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid branchId,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
        => await _dbContext.SubscriptionPlans.AnyAsync(
            p => p.StatusId == (short)EntityStatusEnum.Active
              && p.BranchId == branchId
              && p.Name == name.Trim()
              && (excludeId == null || p.Id != excludeId.Value),
            cancellationToken);

    public void Add(SubscriptionPlan plan)    => _dbContext.SubscriptionPlans.Add(plan);
    public void Update(SubscriptionPlan plan) => _dbContext.SubscriptionPlans.Update(plan);
}
