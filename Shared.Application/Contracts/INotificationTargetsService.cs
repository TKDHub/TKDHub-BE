using Shared.Application.Models;

namespace Shared.Application.Contracts;

/// <summary>
/// Resolves who to notify (SuperAdmins for the tenant, Admins for the branch) from Identity.
/// </summary>
public interface INotificationTargetsService
{
    Task<List<NotificationTarget>> GetAdminsAndSuperAdminsAsync(
        Guid tenantId, Guid branchId, CancellationToken cancellationToken = default);
}
