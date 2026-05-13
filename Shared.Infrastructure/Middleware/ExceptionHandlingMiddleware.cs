using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Models;
using Shared.Domain.Exceptions;

namespace Shared.Infrastructure.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public ExceptionHandlingMiddleware(RequestDelegate next,ILogger<ExceptionHandlingMiddleware> logger,IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context, IErrorLogService errorLogService)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An unhandled exception occurred: {Message}",
                    exception.Message);

                await ForwardErrorLog(context, exception, errorLogService);

                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task ForwardErrorLog(HttpContext context, Exception exception, IErrorLogService errorLogService)
        {
            // Skip health checks and the error-log ingest endpoint itself (prevents Identity self-recursion)
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/api/errorlogs"))
                return;

            try
            {
                var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

                var userId   = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var tenantId = context.User?.FindFirst("TenantId")?.Value;

                var severity = exception switch
                {
                    ValidationException   => "Warning",
                    NotFoundException     => "Information",
                    BusinessRuleException => "Warning",
                    UnauthorizedException => "Warning",
                    ForbiddenException    => "Warning",
                    _                     => "Error"
                };

                string? requestBody = null;
                if (context.Request.ContentLength > 0 && context.Request.Body.CanSeek)
                {
                    context.Request.Body.Position = 0;
                    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                    requestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }

                string? additionalData = null;
                if (exception is ValidationException validationEx)
                    additionalData = JsonSerializer.Serialize(validationEx.Errors);

                var payload = new ErrorLogPayload
                {
                    Id             = Guid.NewGuid(),
                    Message        = exception.Message,
                    StackTrace     = exception.StackTrace,
                    InnerException = exception.InnerException?.ToString(),
                    ExceptionType  = exception.GetType().Name,
                    StatusCode     = GetStatusCode(exception),
                    RequestPath    = context.Request.Path,
                    RequestMethod  = context.Request.Method,
                    QueryString    = context.Request.QueryString.ToString(),
                    RequestBody    = requestBody?.Length > 8000 ? requestBody[..8000] : requestBody,
                    UserAgent      = context.Request.Headers["User-Agent"].ToString(),
                    IpAddress      = context.Connection.RemoteIpAddress?.ToString(),
                    UserId         = userId,
                    TenantId       = tenantId,
                    TraceId        = traceId,
                    Severity       = severity,
                    Timestamp      = DateTimeOffset.UtcNow,
                    AdditionalData = additionalData
                };

                await errorLogService.LogAsync(payload);

                _logger.LogInformation("Error forwarded to Identity log service with ID: {ErrorLogId}", payload.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to forward error log to Identity");
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            var errorResponse = exception switch
            {
                NotFoundException notFoundEx => CreateErrorResponse(
                    "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    "Resource Not Found",
                    (int)HttpStatusCode.NotFound,
                    notFoundEx.Message,
                    context.Request.Path,
                    traceId),

                ValidationException validationEx => CreateErrorResponse(
                    "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    "Validation Error",
                    (int)HttpStatusCode.BadRequest,
                    "One or more validation errors occurred",
                    context.Request.Path,
                    traceId,
                    validationEx.Errors),

                BusinessRuleException businessEx => CreateErrorResponse(
                    "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    "Business Rule Violation",
                    (int)HttpStatusCode.BadRequest,
                    businessEx.Message,
                    context.Request.Path,
                    traceId),

                UnauthorizedException unauthorizedEx => CreateErrorResponse(
                    "https://tools.ietf.org/html/rfc7235#section-3.1",
                    "Unauthorized",
                    (int)HttpStatusCode.Unauthorized,
                    unauthorizedEx.Message,
                    context.Request.Path,
                    traceId),

                ForbiddenException forbiddenEx => CreateErrorResponse(
                    "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    "Forbidden",
                    (int)HttpStatusCode.Forbidden,
                    forbiddenEx.Message,
                    context.Request.Path,
                    traceId),

                DomainException domainEx => CreateErrorResponse(
                    "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    "Domain Error",
                    (int)HttpStatusCode.BadRequest,
                    domainEx.Message,
                    context.Request.Path,
                    traceId),

                _ => CreateErrorResponse(
                    "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    "Internal Server Error",
                    (int)HttpStatusCode.InternalServerError,
                    _environment.IsDevelopment()
                        ? exception.Message
                        : "An error occurred while processing your request",
                    context.Request.Path,
                    traceId)
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = errorResponse.Status;

            var json = JsonSerializer.Serialize(errorResponse, JsonOptions);
            await context.Response.WriteAsync(json);
        }

        private static int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                NotFoundException => (int)HttpStatusCode.NotFound,
                ValidationException => (int)HttpStatusCode.BadRequest,
                BusinessRuleException => (int)HttpStatusCode.BadRequest,
                UnauthorizedException => (int)HttpStatusCode.Unauthorized,
                ForbiddenException => (int)HttpStatusCode.Forbidden,
                DomainException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };
        }

        private static ErrorResponse CreateErrorResponse(
            string type,
            string title,
            int status,
            string? detail,
            string? instance,
            string? traceId,
            Dictionary<string, string[]>? errors = null)
        {
            return ErrorResponse.Create(
                type,
                title,
                status,
                detail,
                instance,
                traceId,
                errors);
        }
    }
}
