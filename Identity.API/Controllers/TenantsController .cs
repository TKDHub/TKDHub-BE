using Identity.Application.Commands.Tenants;
using Identity.Application.Models.Tenant;
using Identity.Application.Queries.Tenants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly ISender _sender;

        public TenantsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTenants(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetAllTenantsQuery(), cancellationToken);

            return Ok(result.Value);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetTenantById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetTenentByIdQuery(id), cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error.Description });
            }

            return Ok(result.Value);
        }

        [HttpGet("subdomain/{subdomain}")]
        public async Task<IActionResult> GetTenantBySubdomain(string subdomain, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetTenentBySubdomainQuery(subdomain), cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error.Description });
            }

            return Ok(result.Value);

        }

        [HttpPost]
        public async Task<IActionResult> CreateTenant([FromBody] TenantModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new CreateTenantCommand(model), cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error.Description });
            }
            return CreatedAtAction(nameof(GetTenantById), new { id = result!.Value.Id }, result.Value);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTenant([FromBody] TenantModel model, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new UpdateTenantCommand(model), cancellationToken);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error.Description });
            }
            return Ok(result.Value);
        }
    }
}