namespace Dojo.API.Settings
{
    public sealed class RestKeySettings
    {
        public const string SectionName = "RestKeySettings";
        public string Key { get; init; } = string.Empty;
        public string HeaderName { get; init; } = "X-Rest-Key";
    }
}
