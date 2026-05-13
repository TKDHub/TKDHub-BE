namespace Dojo.Infrastructure.Settings;

public sealed class IdentityApiSettings
{
    public const string SectionName = "IdentityApiSettings";

    public string BaseUrl { get; init; } = string.Empty;
}
