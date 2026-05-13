namespace Shared.Infrastructure.Settings;

public sealed class ErrorLogSettings
{
    public const string SectionName = "ErrorLogSettings";

    /// <summary>Base URL of the Identity service that hosts POST /api/errorlogs.</summary>
    public string BaseUrl    { get; init; } = string.Empty;

    /// <summary>Shared secret sent as X-Rest-Key for service-to-service auth.</summary>
    public string ServiceKey { get; init; } = string.Empty;
}
