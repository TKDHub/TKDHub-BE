using Identity.Application.Commands.Tenants;
using Identity.Application.Models.Tenant;
using Identity.Application.Queries.Tenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Pagination;

namespace Identity.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TenantsController : BaseApiController
    {
        private readonly ISender _sender;

        public TenantsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllTenants([FromQuery] PagedRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetAllTenantsQuery(request), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return Ok(result.Value);
        }

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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTenant([FromBody] TenantModel model, CancellationToken cancellationToken)
        {
            model.CreatedByEmail = GetUserEmailFromClaims();
            model.CreatedByName = GetUserNameFromClaims();

            var result = await _sender.Send(new CreateTenantCommand(model), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return CreatedAtAction(nameof(GetTenantById), new { id = result.Value.Id }, result.Value);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateTenant([FromBody] TenantModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new UpdateTenantCommand(model), cancellationToken);

            if (result.IsFailure)
                return BadRequest(new { error = result.Error.Description });

            return Ok(result.Value);
        }
    }
}
