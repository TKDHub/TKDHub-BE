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
public class StudentsController : BaseApiController
{
    private readonly ISender        _sender;
    private readonly IBranchContext _branchContext;
    private readonly ITenantContext _tenantContext;

    public StudentsController(ISender sender, IBranchContext branchContext, ITenantContext tenantContext)
    {
        _sender        = sender;
        _branchContext = branchContext;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(PagedResult<StudentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllStudents(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAllStudentsQuery(request), cancellationToken);

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
        var result = await _sender.Send(new GetStudentByIdQuery(id), cancellationToken);

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

        var result = await _sender.Send(
            new CreateStudentCommand(model, _branchContext.BranchId, _tenantContext.TenantId),
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

        var result = await _sender.Send(new UpdateStudentCommand(model), cancellationToken);

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

        var result = await _sender.Send(
            new UploadStudentImageCommand(id, stream, image.FileName, image.ContentType, image.Length),
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
        var result = await _sender.Send(new DeleteStudentCommand(id), cancellationToken);

        if (result.IsFailure)
            return result.Error == StudentErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return NoContent();
    }
}
