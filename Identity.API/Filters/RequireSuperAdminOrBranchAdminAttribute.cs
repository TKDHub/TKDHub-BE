using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Domain.Enums;
using System.Security.Claims;

namespace Identity.API.Filters;

/// <summary>
/// Allows access to SuperAdmin (Admin) unconditionally,
/// or to a SubAdmin who belongs to the branch identified by the route <c>{id}</c> parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireSuperAdminOrBranchAdminAttribute : Attribute, IAsyncActionFilter
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

        // SuperAdmin — allow unconditionally
        if (roleClaims.Contains(nameof(UserRoleEnum.SuberAdmin)))
        {
            await next();
            return;
        }

        // Admin — must belong to the branch in the route
        if (roleClaims.Contains(nameof(UserRoleEnum.Admin)))
        {
            var routeBranchId = context.RouteData.Values["id"]?.ToString();

            var userBranchIds = user.FindAll("BranchId")
                                   .Select(c => c.Value)
                                   .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(routeBranchId) && userBranchIds.Contains(routeBranchId))
            {
                await next();
                return;
            }
        }

        context.Result = new ForbidResult();
    }
}
