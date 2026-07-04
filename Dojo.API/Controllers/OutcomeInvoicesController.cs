using Dojo.API.Filters;
using Dojo.Application.Commands.OutcomeInvoices;
using Dojo.Application.Dtos.OutcomeInvoices;
using Dojo.Application.Models.OutcomeInvoice;
using Dojo.Application.Queries.OutcomeInvoices;
using Dojo.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Contracts;
using Shared.Domain.Pagination;

namespace Dojo.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class OutcomeInvoicesController(ISender sender, IBranchContext branchContext, ITenantContext tenantContext) : BaseApiController
{
    /// <summary>
    /// Returns a paginated list of outcome invoices (branch expenses). SuperAdmin sees
    /// all branches; branch Admins see only their own branch. Filter by any column
    /// (e.g. Title, Amount) through <see cref="PagedRequest.Filters"/>.
    /// </summary>
    [HttpPost("search")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(PagedResult<OutcomeInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllOutcomeInvoices(
        [FromBody] PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAllOutcomeInvoicesQuery(request), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>Returns a single outcome invoice.</summary>
    [HttpGet("{id:guid}")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(OutcomeInvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOutcomeInvoiceById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetOutcomeInvoiceByIdQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>
    /// Records a new outcome invoice (branch expense). Currency is snapshotted from the
    /// current branch. To attach a receipt, upload it separately via
    /// <c>POST /api/OutcomeInvoices/{id}/attachment</c> after creation.
    /// </summary>
    [HttpPost]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(OutcomeInvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOutcomeInvoice(
        [FromBody] CreateOutcomeInvoiceModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            CreatedByEmail = GetUserEmailFromClaims(),
            CreatedByName  = GetUserNameFromClaims()
        };

        var result = await sender.Send(
            new CreateOutcomeInvoiceCommand(model, branchContext.BranchId, tenantContext.TenantId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error == OutcomeInvoiceErrors.BranchNotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return CreatedAtAction(nameof(GetOutcomeInvoiceById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Uploads or replaces the receipt/attachment for an existing outcome invoice.
    /// Accepted: JPEG, PNG, WebP — max 10 MB.
    /// </summary>
    [HttpPost("{id:guid}/attachment")]
    [Consumes("multipart/form-data")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadOutcomeInvoiceAttachment(
        Guid              id,
        IFormFile         attachment,
        CancellationToken cancellationToken = default)
    {
        await using var stream = attachment.OpenReadStream();

        var result = await sender.Send(
            new UploadOutcomeInvoiceAttachmentCommand(id, stream, attachment.FileName, attachment.ContentType, attachment.Length),
            cancellationToken);

        if (result.IsFailure)
            return result.Error == OutcomeInvoiceErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return Ok(new { url = result.Value });
    }

    /// <summary>Soft-deletes an outcome invoice (status -> Deleted). The row is never physically removed.</summary>
    [HttpDelete("{id:guid}")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOutcomeInvoice(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeleteOutcomeInvoiceCommand(id), cancellationToken);

        if (result.IsFailure)
            return result.Error == OutcomeInvoiceErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return NoContent();
    }
}
