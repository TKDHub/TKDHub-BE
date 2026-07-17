using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Application.Contracts;
using Shared.Application.Models;
using Shared.Infrastructure.Settings;

namespace Shared.Infrastructure.Services;

/// <summary>
/// Single, shared implementation of <see cref="IErrorLogService"/>.
/// Every service — including Identity itself — forwards error logs via
/// HTTP POST to Identity's <c>/api/errorlogs</c> endpoint.
/// </summary>
public sealed class HttpErrorLogService : IErrorLogService
{
    private readonly IHttpClientFactory        _httpClientFactory;
    private readonly ErrorLogSettings          _settings;
    private readonly RestKeySettings           _restKey;
    private readonly ILogger<HttpErrorLogService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public HttpErrorLogService(
        IHttpClientFactory httpClientFactory,
        IOptions<ErrorLogSettings> settings,
        IOptions<RestKeySettings> restKey,
        ILogger<HttpErrorLogService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings          = settings.Value;
        _restKey           = restKey.Value;
        _logger            = logger;
    }

    public async Task LogAsync(ErrorLogPayload payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ErrorLogClient");
            client.DefaultRequestHeaders.Remove(_restKey.HeaderName);
            client.DefaultRequestHeaders.Add(_restKey.HeaderName, _restKey.Key);

            var response = await client.PostAsJsonAsync(
                $"{_settings.BaseUrl.TrimEnd('/')}/api/errorlogs",
                payload,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning(
                    "Error log endpoint returned {Status} for log {Id}",
                    (int)response.StatusCode, payload.Id);
        }
        catch (Exception ex)
        {
            // Never let logging failure crash the app
            _logger.LogError(ex, "HttpErrorLogService: failed to forward log {Id}", payload.Id);
        }
    }
}
