using System.Net.Http.Headers;
using System.Text.Json;
using Shared.Application.Contracts;
using Dojo.Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dojo.Infrastructure.Services;

internal sealed class IdentityBranchService : IBranchService
{
    private readonly IHttpClientFactory   _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IdentityApiSettings  _settings;
    private readonly ILogger<IdentityBranchService> _logger;

    public IdentityBranchService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IOptions<IdentityApiSettings> settings,
        ILogger<IdentityBranchService> logger)
    {
        _httpClientFactory   = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _settings            = settings.Value;
        _logger              = logger;
    }

    public async Task<string?> GetCurrencyAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("IdentityApi");

            // Forward the caller's JWT so the Identity endpoint authorises the request
            var token = _httpContextAccessor.HttpContext?
                .Request.Headers.Authorization.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization =
                    AuthenticationHeaderValue.Parse(token);

            var response = await client.GetAsync(
                $"{_settings.BaseUrl.TrimEnd('/')}/api/branches/{branchId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Identity API returned {Status} for branch {BranchId}",
                    (int)response.StatusCode, branchId);

                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return doc.RootElement.GetProperty("currency").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch currency for branch {BranchId}", branchId);
            return null;
        }
    }
}
