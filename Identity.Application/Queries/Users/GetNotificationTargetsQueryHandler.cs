using Identity.Application.Dtos.Users;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Application.Queries.Users;

/// <summary>
/// Resolves who to notify for a tenant/branch: SuperAdmins for the tenant plus Admins for
/// the branch. Called by other services (over the system-to-system REST-key endpoint), not
/// by end users.
/// </summary>
public sealed record GetNotificationTargetsQuery(Guid TenantId, Guid BranchId) : IQuery<List<NotificationTargetDto>>;

internal sealed class GetNotificationTargetsQueryHandler : IQueryHandler<GetNotificationTargetsQuery, List<NotificationTargetDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetNotificationTargetsQueryHandler> _logger;

    public GetNotificationTargetsQueryHandler(IUserRepository userRepository, ILogger<GetNotificationTargetsQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<List<NotificationTargetDto>>> Handle(GetNotificationTargetsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetNotificationTargets: starting for tenant {TenantId}, branch {BranchId}", request.TenantId, request.BranchId);

        var users = await _userRepository.GetAdminsAndSuperAdminsAsync(request.TenantId, request.BranchId, cancellationToken);

        var dtos = users.Select(u => new NotificationTargetDto
        {
            Id          = u.Id,
            Name        = u.Username,
            PhoneNumber = u.PhoneNumber,
            Role        = u.UserRoles.Any(r => r.RoleId == UserRoleEnum.SuberAdmin)
                ? nameof(UserRoleEnum.SuberAdmin)
                : nameof(UserRoleEnum.Admin)
        }).ToList();

        _logger.LogInformation("GetNotificationTargets: resolved {Count} target(s)", dtos.Count);
        return Result.Success(dtos);
    }
}
