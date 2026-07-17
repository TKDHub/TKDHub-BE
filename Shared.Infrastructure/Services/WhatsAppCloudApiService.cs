using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Application.Contracts;
using Shared.Infrastructure.Settings;

namespace Shared.Infrastructure.Services;

/// <summary>
/// Sends WhatsApp text messages via Meta's WhatsApp Cloud API. If credentials aren't
/// configured, sends are skipped (logged, not thrown) — this must never block the caller's
/// own business transaction (e.g. deactivating an expired student).
/// </summary>
public sealed partial class WhatsAppCloudApiService : IWhatsAppService
{
    private readonly IHttpClientFactory              _httpClientFactory;
    private readonly WhatsAppSettings                _settings;
    private readonly ILogger<WhatsAppCloudApiService> _logger;

    public WhatsAppCloudApiService(
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppCloudApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings          = settings.Value;
        _logger            = logger;
    }

    public async Task SendMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AccessToken) || string.IsNullOrWhiteSpace(_settings.PhoneNumberId))
        {
            _logger.LogWarning("WhatsApp is not configured — skipping message to {Phone}", phoneNumber);
            return;
        }

        var to = NormalizePhoneNumber(phoneNumber);
        if (to.Length == 0)
        {
            _logger.LogWarning("Skipping WhatsApp send — no usable phone number");
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("WhatsAppApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);

            var payload = new
            {
                messaging_product = "whatsapp",
                to,
                type = "text",
                text = new { body = message }
            };

            var response = await client.PostAsJsonAsync(
                $"{_settings.ApiUrl.TrimEnd('/')}/{_settings.PhoneNumberId}/messages",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning(
                    "WhatsApp API returned {Status} sending to {Phone}",
                    (int)response.StatusCode, phoneNumber);
        }
        catch (Exception ex)
        {
            // Never let a notification failure abort the caller's own transaction.
            _logger.LogError(ex, "Failed to send WhatsApp message to {Phone}", phoneNumber);
        }
    }

    // WhatsApp Cloud API expects digits only (country code + number, no '+', spaces, or dashes).
    private static string NormalizePhoneNumber(string phoneNumber) => NonDigits().Replace(phoneNumber, "");

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigits();
}
