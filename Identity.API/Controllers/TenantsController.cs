using Identity.API.Filters;
using Identity.Application.Commands.Tenants;
using Identity.Application.Models.Tenant;
using Identity.Application.Queries.Tenants;
using Identity.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Pagination;

namespace Identity.API.Controllers
{
    [Route("api/[controller]")]
    public class TenantsController : BaseApiController
    {
        private readonly ISender _sender;

        public TenantsController(ISender sender)
        {
            _sender = sender;
        }

        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllTenants([FromQuery] PagedRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetAllTenantsQuery(request), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return Ok(result.Value);
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTenantById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetTenantByIdQuery(id), cancellationToken);

            if (result.IsFailure)
                return NotFound(new { error = result.Error.Description });

            return Ok(result.Value);
        }

        [Authorize]
        [HttpGet("subdomain/{subdomain}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTenantBySubdomain(string subdomain, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetTenantBySubdomainQuery(subdomain), cancellationToken);

            if (result.IsFailure)
                return NotFound(new { error = result.Error.Description });

            return Ok(result.Value);
        }

        [RequireRestKey]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateTenant([FromBody] TenantModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new CreateTenantCommand(model), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return CreatedAtAction(nameof(GetTenantById), new { id = result.Value.Id }, result.Value);
        }

        [RequireRestKey]
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTenant([FromBody] TenantModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new UpdateTenantCommand(model), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return Ok(result.Value);
        }

        [RequireRestKey]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTenant(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new DeleteTenantCommand(id), cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error == TenantErrors.NotFound)
                    return NotFound(new { error = result.Error.Description });

                return BadRequest(new { error = result.Error.Description });
            }

            return NoContent();
        }
    }
}
