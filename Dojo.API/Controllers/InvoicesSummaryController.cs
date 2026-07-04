using Dojo.API.Filters;
using Dojo.Application.Dtos.InvoicesSummary;
using Dojo.Application.Queries.InvoicesSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Pagination;

namespace Dojo.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class InvoicesSummaryController(ISender sender) : BaseApiController
{
    /// <summary>
    /// Dashboard-style summary combining income and outcome invoices for the current
    /// branch (SuperAdmin spans all branches). Returns net income collected (Paid minus
    /// Refund transactions), total active outcome, and the paged detail lists for both —
    /// same DTO shape as each entity's GetById. Filter via <see cref="PagedRequest.Filters"/>;
    /// totals aggregate the full filtered set, the lists are paged.
    /// </summary>
    [HttpPost("search")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(InvoicesSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInvoicesSummary(
        [FromBody] PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetInvoicesSummaryQuery(request), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }
}
