namespace Shared.Infrastructure.Extensions
{
    /// <summary>
    /// Metadata describing a microservice API, used to configure Swagger and routing.
    /// </summary>
    public sealed record ApiInfo(
        string Title,
        string Version,
        string Description,
        string RoutePrefix);
}
