using Dojo.API.Filters;
using Dojo.Application.Commands.Classes;
using Dojo.Application.Dtos.Classes;
using Dojo.Application.Models.Class;
using Dojo.Application.Queries.Classes;
using Dojo.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Contracts;
using Shared.Domain.Pagination;

namespace Dojo.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class ClassesController(ISender sender, IBranchContext branchContext, ITenantContext tenantContext) : BaseApiController
{
    /// <summary>Returns a paginated list of classes for the current branch.</summary>
    [HttpPost("search")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(PagedResult<ClassDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllClasses(
        [FromBody] PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAllClassesQuery(request), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>Returns a single class by ID, including its currently enrolled students.</summary>
    [HttpGet("{id:guid}")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(ClassDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClassById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetClassByIdQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>Creates a new class. Any students given are enrolled immediately (moving them off any prior class).</summary>
    [HttpPost]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(ClassDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateClass(
        [FromBody] ClassModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            CreatedByEmail = GetUserEmailFromClaims(),
            CreatedByName  = GetUserNameFromClaims()
        };

        var result = await sender.Send(
            new CreateClassCommand(model, branchContext.BranchId, tenantContext.TenantId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error == ClassErrors.NameAlreadyExists
                ? Conflict(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return CreatedAtAction(nameof(GetClassById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>Updates a class's name, time window, and weekdays. The roster is managed via the dedicated endpoints below.</summary>
    [HttpPut("{id:guid}")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(ClassDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateClass(
        Guid id,
        [FromBody] ClassModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            ClassId         = id,
            ModifiedByEmail = GetUserEmailFromClaims(),
            ModifiedByName  = GetUserNameFromClaims()
        };

        var result = await sender.Send(new UpdateClassCommand(model), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == ClassErrors.NotFound)
                return NotFound(new { error = result.Error.Description });

            if (result.Error == ClassErrors.NameAlreadyExists)
                return Conflict(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>Soft-deletes a class. Fails if the class still has at least one active student.</summary>
    [HttpDelete("{id:guid}")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteClass(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DeleteClassCommand(id), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == ClassErrors.NotFound)
                return NotFound(new { error = result.Error.Description });

            if (result.Error == ClassErrors.HasActiveStudents)
                return Conflict(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return NoContent();
    }

    /// <summary>Enrolls students into this class. A student already in another class is moved, not rejected.</summary>
    [HttpPost("{id:guid}/students")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(ClassDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddStudentsToClass(
        Guid id,
        [FromBody] List<Guid> studentIds,
        CancellationToken cancellationToken = default)
    {
        var model = new AddStudentsToClassModel
        {
            ClassId          = id,
            StudentIds       = studentIds,
            PerformedByEmail = GetUserEmailFromClaims(),
            PerformedByName  = GetUserNameFromClaims()
        };
        var result = await sender.Send(new AddStudentsToClassCommand(model), cancellationToken);

        if (result.IsFailure)
            return result.Error == ClassErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>Removes students from this class (students not currently in it are ignored).</summary>
    [HttpPost("{id:guid}/students/remove")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(ClassDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveStudentsFromClass(
        Guid id,
        [FromBody] List<Guid> studentIds,
        CancellationToken cancellationToken = default)
    {
        var model = new RemoveStudentsFromClassModel
        {
            ClassId          = id,
            StudentIds       = studentIds,
            PerformedByEmail = GetUserEmailFromClaims(),
            PerformedByName  = GetUserNameFromClaims()
        };
        var result = await sender.Send(new RemoveStudentsFromClassCommand(model), cancellationToken);

        if (result.IsFailure)
            return result.Error == ClassErrors.NotFound
                ? NotFound(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    /// <summary>Moves students from one class to another.</summary>
    [HttpPost("move-students")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(ClassDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveStudentsToClass(
        [FromBody] MoveStudentsToClassModel model,
        CancellationToken cancellationToken = default)
    {
        model = model with
        {
            PerformedByEmail = GetUserEmailFromClaims(),
            PerformedByName  = GetUserNameFromClaims()
        };

        var result = await sender.Send(new MoveStudentsToClassCommand(model), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == ClassErrors.NotFound || result.Error == ClassErrors.TargetClassNotFound)
                return NotFound(new { error = result.Error.Description });

            return BadRequest(new { error = result.Error.Description });
        }

        return Ok(result.Value);
    }

    /// <summary>Report: every student alongside their currently linked class, belt level, and age.</summary>
    [HttpPost("students/search")]
    [RequireSuperAdminOrBranchAdmin]
    [ProducesResponseType(typeof(PagedResult<StudentClassSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudentsWithClasses(
        [FromBody] PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetStudentsWithClassesQuery(request), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }
}
