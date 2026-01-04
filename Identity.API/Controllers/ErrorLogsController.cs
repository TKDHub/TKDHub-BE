using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Repositories;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ErrorLogsController : ControllerBase
    {
        private readonly IErrorLogRepository _errorLogRepository;

        public ErrorLogsController(IErrorLogRepository errorLogRepository)
        {
            _errorLogRepository = errorLogRepository;
        }

        //[HttpGet]
        //public async Task<IActionResult> GetAllErrors([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
        //{
        //    var errors = await _errorLogRepository.GetAllAsync(
        //        pageNumber,
        //        pageSize,
        //        cancellationToken);

        //    var totalCount = await _errorLogRepository.GetCountAsync(cancellationToken);

        //    return Ok(new
        //    {
        //        data = errors,
        //        pageNumber,
        //        pageSize,
        //        totalCount,
        //        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        //    });
        //}

        //[HttpGet("unresolved")]
        //public async Task<IActionResult> GetUnresolvedErrors(CancellationToken cancellationToken)
        //{
        //    var errors = await _errorLogRepository.GetUnresolvedAsync(cancellationToken);
        //    return Ok(errors);
        //}

        //[HttpGet("{id:guid}")]
        //public async Task<IActionResult> GetErrorById(Guid id, CancellationToken cancellationToken)
        //{
        //    var error = await _errorLogRepository.GetByIdAsync(id, cancellationToken);

        //    if (error is null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(error);
        //}

        //[HttpPost("{id:guid}/resolve")]
        //public async Task<IActionResult> ResolveError(Guid id, [FromBody] ResolveErrorRequest request, CancellationToken cancellationToken)
        //{
        //    var error = await _errorLogRepository.GetByIdAsync(id, cancellationToken);

        //    if (error is null)
        //    {
        //        return NotFound();
        //    }

        //    var userName = User.Identity?.Name ?? "Unknown";
        //    error.Resolve(userName, request.Notes);

        //    await _errorLogRepository.SaveChangesAsync(cancellationToken);

        //    return Ok(new { message = "Error resolved successfully" });
        //}
    }

    public sealed record ResolveErrorRequest
    {
        public string? Notes { get; init; }
    }
}