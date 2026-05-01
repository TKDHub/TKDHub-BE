using Identity.API.Filters;
using Identity.Application.Commands.Branches;
using Identity.Application.Dtos.Branches;
using Identity.Application.Models.Branch;
using Identity.Application.Queries.Branches;
using Identity.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Pagination;

namespace Identity.API.Controllers;

[Route("api/[controller]")]
public class BranchesController : BaseApiController
{
    private readonly ISender _sender;

    public BranchesController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BranchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBranches([FromQuery] PagedRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAllBranchesQuery(request), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBranchById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBranchByIdQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [RequireRestKey]
    [HttpPost]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBranch([FromBody] BranchModel model, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateBranchCommand(model), cancellationToken);

        if (result.IsFailure)
            return result.Error == BranchErrors.NameAlreadyExists
                ? Conflict(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return CreatedAtAction(nameof(GetBranchById), new { id = result.Value.Id }, result.Value);
    }

    [RequireRestKey]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateBranch(Guid id, [FromBody] BranchModel model, CancellationToken cancellationToken)
    {
        model.BranchId = id;

        var result = await _sender.Send(new UpdateBranchCommand(model), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == BranchErrors.NotFound)
                return NotFound(new { error = result.Error.Description });

            if (result.Error == BranchErrors.NameAlreadyExists)
                return Conflict(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    [RequireRestKey]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBranch(Guid id, [FromQuery] bool deleteRecursively = false, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new DeleteBranchCommand(id, deleteRecursively), cancellationToken);

        if (result.IsFailure)
            return result.Error == BranchErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return NoContent();
    }
}
