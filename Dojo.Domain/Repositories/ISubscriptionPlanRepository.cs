using Dojo.Domain.Entities;
using Shared.Domain.Pagination;

namespace Dojo.Domain.Repositories;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SubscriptionPlan?> GetByIdWithStudentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<SubscriptionPlan>> GetPagedAsync(PagedRequest request, string? status, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid branchId, Guid? excludeId, CancellationToken cancellationToken = default);
    void Add(SubscriptionPlan plan);
    void Update(SubscriptionPlan plan);
}
