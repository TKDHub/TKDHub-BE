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
public class IncomeInvoicesController(ISender sender) : BaseApiController
{
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
        var result = await sender.Send(new GetAllIncomeInvoicesQuery(request), cancellationToken);

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
        var result = await sender.Send(new GetIncomeInvoiceByIdQuery(id), cancellationToken);

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

        var result = await sender.Send(new CreateIncomeInvoiceCommand(model), cancellationToken);

        if (result.IsFailure)
            return result.Error == IncomeInvoiceErrors.StudentNotFound || result.Error == IncomeInvoiceErrors.BranchNotFound
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

        var result = await sender.Send(new AddIncomeTransactionCommand(model), cancellationToken);

        if (result.IsFailure)
            return result.Error == IncomeInvoiceErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>
    /// Voids an invoice. Every Paid transaction still holding an unrefunded balance
    /// gets a matching Refund transaction for that remaining amount — the original
    /// Paid transactions are never modified. The invoice itself is never chased for
    /// payment again once voided.
    /// </summary>
    [HttpPost("{id:guid}/void")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(IncomeInvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VoidIncomeInvoice(
        Guid id,
        [FromBody] VoidIncomeInvoiceModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            InvoiceId     = id,
            VoidedByEmail = GetUserEmailFromClaims(),
            VoidedByName  = GetUserNameFromClaims()
        };

        var result = await sender.Send(new VoidIncomeInvoiceCommand(model), cancellationToken);

        if (result.IsFailure)
            return result.Error == IncomeInvoiceErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>
    /// Refunds part or all of a single Paid transaction. InvoiceId and TransactionId
    /// are supplied in the body. Creates a new Refund transaction rather than mutating
    /// the original, and can reopen an invoice that was previously Closed if the
    /// refunded amount leaves a balance due again.
    /// </summary>
    [HttpPost("transactions/refund")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(IncomeInvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefundIncomeTransaction(
        [FromBody] RefundIncomeTransactionModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            RefundedByEmail = GetUserEmailFromClaims(),
            RefundedByName  = GetUserNameFromClaims()
        };

        var result = await sender.Send(new RefundIncomeTransactionCommand(model), cancellationToken);

        if (result.IsFailure)
            return result.Error == IncomeInvoiceErrors.NotFound || result.Error == IncomeInvoiceErrors.TransactionNotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }
}
