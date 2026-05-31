using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Application.Contracts;
using Shared.Domain.Enums;
using System.Security.Claims;

namespace Dojo.API.Filters;

/// <summary>
/// SuperAdmin (Admin) passes unconditionally.
/// SubAdmin passes only if they belong to the branch in the <c>X-Branch-Id</c> header.
/// All other roles are denied.
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

        // ── SuperAdmin — allow unconditionally ────────────────────────────────
        if (roleClaims.Contains(nameof(UserRoleEnum.SuberAdmin)))
        {
            await next();
            return;
        }

        // ── Admin — must belong to the current branch ──────────────────────
        if (roleClaims.Contains(nameof(UserRoleEnum.Admin)))
        {
            var branchContext = context.HttpContext.RequestServices.GetRequiredService<IBranchContext>();

            var userBranchIds = user.FindAll("BranchId")
                                   .Select(c => c.Value)
                                   .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (userBranchIds.Contains(branchContext.BranchId.ToString()))
            {
                await next();
                return;
            }
        }

        context.Result = new ForbidResult();
    }
}
