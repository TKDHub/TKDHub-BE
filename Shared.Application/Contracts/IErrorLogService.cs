using Shared.Application.Models;

namespace Shared.Application.Contracts;

/// <summary>
/// Forwards error log entries to the central Identity logging endpoint.
/// </summary>
public interface IErrorLogService
{
    Task LogAsync(ErrorLogPayload payload, CancellationToken cancellationToken = default);
}
