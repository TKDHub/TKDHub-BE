using Shared.Domain.Primitives;

namespace Identity.Domain.Constants
{
    public static class ApiErrors
    {
        public static readonly Error MissingRestKey = new(
            "Api.MissingRestKey",
            "Missing or invalid API key");
    }
}
