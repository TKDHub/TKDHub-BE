using Dojo.API.Filters;
using Dojo.Application.Commands.IncomeInvoices;
using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Models.IncomeInvoice;
using Dojo.Application.Queries.IncomeInvoices;
using Dojo.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Pagination;

namespace Dojo.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class IncomeInvoicesController : BaseApiController
{
    private readonly ISender _sender;

    public IncomeInvoicesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Returns a paginated list of income invoices. SuperAdmin sees all branches;
    /// branch Admins see only their own branch. The paging/sorting/filtering request
    /// is sent in the body — filter by any column through <see cref="PagedRequest.Filters"/>,
    /// e.g. Column "StudentId" / "Type" / "Status", Operator "Equals", with the value.
    /// </summary>
    [HttpPost("search")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(PagedResult<IncomeInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllIncomeInvoices(
        [FromBody] PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAllIncomeInvoicesQuery(request), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>Returns a single income invoice with its transactions.</summary>
    [HttpGet("{id:guid}")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(IncomeInvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncomeInvoiceById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetIncomeInvoiceByIdQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>
    /// Creates an income invoice and, when an amount is collected, records the first
    /// transaction. The backend derives the payment status and Open/Closed state from
    /// the amount paid against the computed price-after-discount.
    /// </summary>
    [HttpPost]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(IncomeInvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateIncomeInvoice(
        [FromBody] CreateIncomeInvoiceModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            CreatedByEmail = GetUserEmailFromClaims(),
            CreatedByName  = GetUserNameFromClaims()
        };

        var result = await _sender.Send(new CreateIncomeInvoiceCommand(model), cancellationToken);

        if (result.IsFailure)
            return result.Error == IncomeInvoiceErrors.StudentNotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return CreatedAtAction(nameof(GetIncomeInvoiceById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Records a subsequent payment against an open income invoice. Auto-closes the
    /// invoice when the sum of transactions covers the amount due.
    /// </summary>
    [HttpPost("{id:guid}/transactions")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(IncomeInvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddIncomeTransaction(
        Guid id,
        [FromBody] AddIncomeTransactionModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            IncomeInvoiceId = id,
            CreatedByEmail  = GetUserEmailFromClaims(),
            CreatedByName   = GetUserNameFromClaims()
        };

        var result = await _sender.Send(new AddIncomeTransactionCommand(model), cancellationToken);

        if (result.IsFailure)
            return result.Error == IncomeInvoiceErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }
}
