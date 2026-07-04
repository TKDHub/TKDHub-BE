using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Domain.Enums;
using System.Security.Claims;

namespace Identity.API.Filters;

/// <summary>
/// Restricts access to SuperAdmin (Admin role) users only.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireSuperAdminAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.Identity is not { IsAuthenticated: true })
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var roleClaims = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();

        if (!roleClaims.Contains(((short)UserRoleEnum.SuberAdmin).ToString()))
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
