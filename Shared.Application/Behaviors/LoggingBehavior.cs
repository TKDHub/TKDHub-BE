using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Domain.Primitives;

namespace Shared.Application.Behaviors;

/// <summary>
/// Pipeline behavior for logging requests and responses
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation(
            "Processing request {RequestName}",
            requestName);

        var result = await next();

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Completed request {RequestName}",
                requestName);
        }
        else
        {
            // A Result.Failure here is an expected business outcome (not found, validation,
            // conflict, etc.), not a bug — Warning, not Error. Unhandled exceptions are a
            // separate case already logged at Error by ExceptionHandlingMiddleware.
            _logger.LogWarning(
                "Request {RequestName} failed with error: {Error}",
                requestName,
                result.Error);
        }

        return result;
    }
}
