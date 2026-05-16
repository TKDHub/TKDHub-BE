using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Application.Contracts;
using Shared.Application.Models;
using Dojo.Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dojo.Infrastructure.Services;

internal sealed class IdentityBranchService : IBranchService
{
    private readonly IHttpClientFactory             _httpClientFactory;
    private readonly IHttpContextAccessor           _httpContextAccessor;
    private readonly IdentityApiSettings            _settings;
    private readonly ILogger<IdentityBranchService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
    };

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

    public async Task<BranchInfo?> GetBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateAuthorizedClient();

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
            return await JsonSerializer.DeserializeAsync<BranchInfo>(stream, JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch branch {BranchId}", branchId);
            return null;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private HttpClient CreateAuthorizedClient()
    {
        var client = _httpClientFactory.CreateClient("IdentityApi");

        var token = _httpContextAccessor.HttpContext?
            .Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization =
                AuthenticationHeaderValue.Parse(token);

        return client;
    }
}
