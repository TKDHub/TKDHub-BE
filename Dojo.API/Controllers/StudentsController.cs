using Dojo.API.Filters;
using Dojo.Application.Commands.Students;
using Dojo.Application.Dtos.Students;
using Dojo.Application.Models.Student;
using Dojo.Application.Queries.Students;
using Dojo.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Pagination;

namespace Dojo.API.Controllers;

[Route("api/[controller]")]
public class StudentsController : BaseApiController
{
    private readonly ISender _sender;

    public StudentsController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StudentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllStudents([FromQuery] PagedRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAllStudentsQuery(request), cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetStudentByIdQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error.Description });

        return Ok(result.Value);
    }

    [RequireRestKey]
    [HttpPost]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStudent([FromBody] StudentModel model, CancellationToken cancellationToken)
    {
        model.CreatedByEmail = GetUserEmailFromClaims();
        model.CreatedByName  = GetUserNameFromClaims();

        var result = await _sender.Send(new CreateStudentCommand(model), cancellationToken);

        if (result.IsFailure)
            return result.Error == StudentErrors.EmailAlreadyExists
                ? Conflict(new { error = result.Error.Description })
                : BadRequest(new { error = result.Error.Description });

        return CreatedAtAction(nameof(GetStudentById), new { id = result.Value.Id }, result.Value);
    }

    [RequireRestKey]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] StudentModel model, CancellationToken cancellationToken)
    {
        model.StudentId      = id;
        model.ModifiedByEmail = GetUserEmailFromClaims();
        model.ModifiedByName  = GetUserNameFromClaims();

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

    [RequireRestKey]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
