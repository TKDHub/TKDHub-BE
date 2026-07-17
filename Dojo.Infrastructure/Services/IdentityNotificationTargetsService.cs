using System.Text.Json;
using System.Text.Json.Serialization;
using Dojo.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Application.Contracts;
using Shared.Application.Models;
using Shared.Infrastructure.Settings;

namespace Dojo.Infrastructure.Services;

/// <summary>
/// Calls Identity's notification-targets endpoint. Unlike <see cref="IdentityBranchService"/>,
/// this is invoked from the student-expiry background job — there's no HTTP request to forward
/// a JWT from, so it authenticates with the shared X-Rest-Key instead.
/// </summary>
internal sealed class IdentityNotificationTargetsService : INotificationTargetsService
{
    private readonly IHttpClientFactory                          _httpClientFactory;
    private readonly IdentityApiSettings                         _settings;
    private readonly RestKeySettings                             _restKey;
    private readonly ILogger<IdentityNotificationTargetsService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
    };

    public IdentityNotificationTargetsService(
        IHttpClientFactory httpClientFactory,
        IOptions<IdentityApiSettings> settings,
        IOptions<RestKeySettings> restKey,
        ILogger<IdentityNotificationTargetsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings          = settings.Value;
        _restKey           = restKey.Value;
        _logger            = logger;
    }

    public async Task<List<NotificationTarget>> GetAdminsAndSuperAdminsAsync(
        Guid tenantId, Guid branchId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("IdentityApi");
            client.DefaultRequestHeaders.Remove(_restKey.HeaderName);
            client.DefaultRequestHeaders.Add(_restKey.HeaderName, _restKey.Key);

            var response = await client.GetAsync(
                $"{_settings.BaseUrl.TrimEnd('/')}/api/user/notification-targets?tenantId={tenantId}&branchId={branchId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Identity API returned {Status} for notification targets (tenant {TenantId}, branch {BranchId})",
                    (int)response.StatusCode, tenantId, branchId);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var targets = await JsonSerializer.DeserializeAsync<List<NotificationTarget>>(stream, JsonOptions, cancellationToken);
            return targets ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch notification targets (tenant {TenantId}, branch {BranchId})", tenantId, branchId);
            return [];
        }
    }
}
