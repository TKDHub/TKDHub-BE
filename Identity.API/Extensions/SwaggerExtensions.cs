using Shared.Infrastructure.Extensions;

namespace Identity.API.Extensions
{
    public static class ApiMetadata
    {
        public static readonly ApiInfo ApiInfo = new(
            Title:       "Identity API",
            Version:     "v1",
            Description: "Identity Microservice — authentication, tenants, branches",
            RoutePrefix: "Identity");
    }
}
