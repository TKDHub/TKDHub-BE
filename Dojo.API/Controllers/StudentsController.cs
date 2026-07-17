using Dojo.API.Filters;
using Dojo.Application.Commands.Students;
using Dojo.Application.Dtos.Students;
using Dojo.Application.Models.Student;
using Dojo.Application.Queries.Students;
using Dojo.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Contracts;
using Shared.Domain.Pagination;

namespace Dojo.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class StudentsController(ISender sender, IBranchContext branchContext, ITenantContext tenantContext) : BaseApiController
{
    [HttpPost("search")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(PagedResult<StudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllStudents(
        [FromBody] PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAllStudentsQuery(request), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetStudentByIdQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [HttpPost]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStudent(
        [FromBody] StudentModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            CreatedByEmail = GetUserEmailFromClaims(),
            CreatedByName  = GetUserNameFromClaims()
        };

        var result = await sender.Send(
            new CreateStudentCommand(model, branchContext.BranchId, tenantContext.TenantId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error == StudentErrors.EmailAlreadyExists
                ? Conflict(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return CreatedAtAction(nameof(GetStudentById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStudent(
        Guid id,
        [FromBody] StudentModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            StudentId       = id,
            ModifiedByEmail = GetUserEmailFromClaims(),
            ModifiedByName  = GetUserNameFromClaims()
        };

        var result = await sender.Send(new UpdateStudentCommand(model), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == StudentErrors.NotFound)
                return NotFound(new { error = result.Error.Description });

            if (result.Error == StudentErrors.EmailAlreadyExists)
                return Conflict(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Uploads or replaces the student's profile image.
    /// Accepted: JPEG, PNG, WebP — max 10 MB.
    /// Returns the Cloudinary delivery URL saved on the student record.
    /// </summary>
    [HttpPost("{id:guid}/image")]
    [RequireSuperAdminOrBranchAdmin]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadStudentImage(
        Guid              id,
        IFormFile         image,
        CancellationToken cancellationToken = default)
    {
        await using var stream = image.OpenReadStream();

        var result = await sender.Send(
            new UploadStudentImageCommand(
                id, stream, image.FileName, image.ContentType, image.Length,
                GetUserEmailFromClaims(), GetUserNameFromClaims()),
            cancellationToken);

        if (result.IsFailure)
            return result.Error == StudentErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return Ok(new { url = result.Value });
    }

    [HttpDelete("{id:guid}")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStudent(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new DeleteStudentCommand(id, GetUserEmailFromClaims(), GetUserNameFromClaims()),
            cancellationToken);

        if (result.IsFailure)
            return result.Error == StudentErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return NoContent();
    }

    /// <summary>
    /// Freezes a student's membership. Snapshots the days remaining until their EndDate
    /// so a future unfreeze can resume the clock from where it was paused.
    /// </summary>
    [HttpPatch("{id:guid}/freeze")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FreezeStudent(Guid id, CancellationToken cancellationToken = default)
    {
        var model = new FreezeStudentModel
        {
            StudentId     = id,
            FrozenByEmail = GetUserEmailFromClaims(),
            FrozenByName  = GetUserNameFromClaims()
        };

        var result = await sender.Send(new FreezeStudentCommand(model), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == StudentErrors.NotFound)
                return NotFound(new { error = result.Error.Description });

            if (result.Error == StudentErrors.AlreadyFrozen)
                return Conflict(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Reactivates a Frozen or Inactive student. A frozen student resumes the clock from the
    /// days remaining at freeze time; an inactive student re-registers against a subscription
    /// plan (theirs, unless a different one is given), exactly like a fresh CreateStudent.
    /// </summary>
    [HttpPatch("{id:guid}/reactivate")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateStudent(
        Guid id,
        [FromBody] ReactivateStudentModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            StudentId       = id,
            ModifiedByEmail = GetUserEmailFromClaims(),
            ModifiedByName  = GetUserNameFromClaims()
        };

        var result = await sender.Send(new ReactivateStudentCommand(model), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == StudentErrors.NotFound)
                return NotFound(new { error = result.Error.Description });

            if (result.Error == StudentErrors.AlreadyActive)
                return Conflict(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>Returns a paginated activity log (create/update/freeze/class changes/etc.) for a single student.</summary>
    [HttpPost("{id:guid}/activity-logs/search")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(PagedResult<StudentActivityLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentActivityLogs(
        Guid id,
        [FromBody] PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetStudentActivityLogsQuery(id, request), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }
}
