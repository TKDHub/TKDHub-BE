namespace Shared.Infrastructure.Settings;

/// <summary>
/// The one shared secret used for all system-to-system calls between services (validated by
/// RequireRestKeyAttribute on the receiving end, sent as the configured header by every
/// outbound caller — HttpErrorLogService, IdentityNotificationTargetsService, etc.).
/// </summary>
public sealed class RestKeySettings
{
    public const string SectionName = "RestKeySettings";

    public string Key        { get; init; } = string.Empty;
    public string HeaderName { get; init; } = "X-Rest-Key";
}
