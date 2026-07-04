using Dojo.API.Filters;
using Dojo.Application.Commands.SubscriptionPlans;
using Dojo.Application.Dtos.SubscriptionPlans;
using Dojo.Application.Models.SubscriptionPlan;
using Dojo.Application.Queries.SubscriptionPlans;
using Dojo.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Contracts;
using Shared.Domain.Pagination;

namespace Dojo.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class SubscriptionPlansController(ISender sender, IBranchContext branchContext, ITenantContext tenantContext) : BaseApiController
{
    /// <summary>
    /// Returns a paginated list of subscription plans for the current branch.
    /// The paging/sorting/filtering request (including any status filter) is sent in the body.
    /// </summary>
    [HttpPost("search")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(PagedResult<SubscriptionPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllSubscriptionPlans(
        [FromBody] PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAllSubscriptionPlansQuery(request), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>Returns a single subscription plan by ID.</summary>
    [HttpGet("{id:guid}")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionPlanById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetSubscriptionPlanByIdQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>Creates a new subscription plan for the branch specified in the <c>X-Branch-Id</c> header.</summary>
    [HttpPost]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSubscriptionPlan(
        [FromBody] SubscriptionPlanModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            CreatedByEmail = GetUserEmailFromClaims(),
            CreatedByName  = GetUserNameFromClaims()
        };

        var result = await sender.Send(new CreateSubscriptionPlanCommand(model, branchContext.BranchId, tenantContext.TenantId), cancellationToken);

        if (result.IsFailure)
            return result.Error == SubscriptionPlanErrors.NameAlreadyExists
                ? Conflict(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return CreatedAtAction(nameof(GetSubscriptionPlanById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>Updates an existing subscription plan.</summary>
    [HttpPut("{id:guid}")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSubscriptionPlan(
        Guid id,
        [FromBody] SubscriptionPlanModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            PlanId          = id,
            ModifiedByEmail = GetUserEmailFromClaims(),
            ModifiedByName  = GetUserNameFromClaims()
        };

        var result = await sender.Send(new UpdateSubscriptionPlanCommand(model), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == SubscriptionPlanErrors.NotFound)
                return NotFound(new { error = result.Error.Description });

            if (result.Error == SubscriptionPlanErrors.NameAlreadyExists)
                return Conflict(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>Restores an archived subscription plan back to active.</summary>
    [HttpPatch("{id:guid}/restore")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreSubscriptionPlan(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new RestoreSubscriptionPlanCommand(id), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == SubscriptionPlanErrors.NotFound)
                return NotFound(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return NoContent();
    }

    /// <summary>Archives a subscription plan (soft-disable). Plans are never hard-deleted.</summary>
    [HttpDelete("{id:guid}")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveSubscriptionPlan(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ArchiveSubscriptionPlanCommand(id), cancellationToken);

        if (result.IsFailure)
            return result.Error == SubscriptionPlanErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return NoContent();
    }
}
