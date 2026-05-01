namespace Identity.API.Settings
{
    public sealed class RestKeySettings
    {
        public const string SectionName = "RestKeySettings";
        public string Key { get; init; } = "211a31ad9a7974f2d1a623b53b7078ee5928a26028d0eb0464a40f870bada1727966f02422062a7204c668f82745c36c";
        public string HeaderName { get; init; } = "X-Rest-Key";
    }
}
