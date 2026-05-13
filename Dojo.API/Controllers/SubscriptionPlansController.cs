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
public class SubscriptionPlansController : BaseApiController
{
    private readonly ISender        _sender;
    private readonly IBranchContext _branchContext;
    private readonly ITenantContext _tenantContext;

    public SubscriptionPlansController(ISender sender, IBranchContext branchContext, ITenantContext tenantContext)
    {
        _sender        = sender;
        _branchContext = branchContext;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Returns a paginated list of subscription plans for the current branch.
    /// Optionally filter by active/archived status using the <c>status</c> query parameter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SubscriptionPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSubscriptionPlans(
        [FromQuery] PagedRequest request,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAllSubscriptionPlansQuery(request, status), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>Returns a single subscription plan by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionPlanById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetSubscriptionPlanByIdQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>Creates a new subscription plan for the branch specified in the <c>X-Branch-Id</c> header.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        var result = await _sender.Send(new CreateSubscriptionPlanCommand(model, _branchContext.BranchId, _tenantContext.TenantId), cancellationToken);

        if (result.IsFailure)
            return result.Error == SubscriptionPlanErrors.NameAlreadyExists
                ? Conflict(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return CreatedAtAction(nameof(GetSubscriptionPlanById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>Updates an existing subscription plan.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        var result = await _sender.Send(new UpdateSubscriptionPlanCommand(model), cancellationToken);

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

    /// <summary>Archives a subscription plan (soft-disable). Plans are never hard-deleted.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveSubscriptionPlan(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ArchiveSubscriptionPlanCommand(id), cancellationToken);

        if (result.IsFailure)
            return result.Error == SubscriptionPlanErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return NoContent();
    }
}
