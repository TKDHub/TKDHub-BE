using Identity.API.Settings;
using Identity.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Identity.API.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class RequireRestKeyAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var settings = context.HttpContext.RequestServices
                .GetRequiredService<IOptions<RestKeySettings>>().Value;

            var hasKey = context.HttpContext.Request.Headers
                .TryGetValue(settings.HeaderName, out var providedKey);

            if (!hasKey || !string.Equals(providedKey, settings.Key, StringComparison.Ordinal))
            {
                context.Result = new UnauthorizedObjectResult(new { error = ApiErrors.MissingRestKey.Description });
                return;
            }

            await next();
        }
    }
}
