using Shared.Infrastructure.Extensions;

namespace Dojo.API.Extensions
{
    public static class ApiMetadata
    {
        public static readonly ApiInfo ApiInfo = new(
            Title:       "Dojo API",
            Version:     "v1",
            Description: "Dojo Microservice — karate dojo management (students, classes, invoices)",
            RoutePrefix: "Dojo");
    }
}
