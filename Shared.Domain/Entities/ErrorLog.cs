using Shared.Domain.Primitives;

namespace Shared.Domain.Entities
{
    public sealed class ErrorLog : BaseEntity<Guid>
    {
        private ErrorLog(
            Guid id,
            string message,
            string? stackTrace,
            string? innerException,
            string exceptionType,
            string? requestPath,
            string? requestMethod,
            string? userId,
            string? tenantId,
            string severity)
        {
            Id = id;
            Message = message;
            StackTrace = stackTrace;
            InnerException = innerException;
            ExceptionType = exceptionType;
            RequestPath = requestPath;
            RequestMethod = requestMethod;
            UserId = userId;
            TenantId = tenantId;
            Severity = severity;
            Timestamp = DateTimeOffset.UtcNow;
            IsResolved = false;
        }

        private ErrorLog() { }

        public string Message { get; private set; } = string.Empty;
        public string? StackTrace { get; private set; }
        public string? InnerException { get; private set; }
        public string ExceptionType { get; private set; } = string.Empty;
        public int? StatusCode { get; private set; }
        public string? RequestPath { get; private set; }
        public string? RequestMethod { get; private set; }
        public string? QueryString { get; private set; }
        public string? RequestBody { get; private set; }
        public string? UserAgent { get; private set; }
        public string? IpAddress { get; private set; }
        public string? UserId { get; private set; }
        public string? TenantId { get; private set; }
        public string? TraceId { get; private set; }
        public string Severity { get; private set; } = string.Empty;
        public DateTimeOffset Timestamp { get; private set; }
        public bool IsResolved { get; private set; }
        public string? ResolutionNotes { get; private set; }
        public DateTimeOffset? ResolvedAt { get; private set; }
        public string? ResolvedBy { get; private set; }
        public string? AdditionalData { get; private set; }

        public static ErrorLog Create(
            string message,
            string? stackTrace,
            string? innerException,
            string exceptionType,
            string? requestPath,
            string? requestMethod,
            string? userId,
            string? tenantId,
            string severity = "Error")
        {
            return new ErrorLog(
                Guid.NewGuid(),
                message,
                stackTrace,
                innerException,
                exceptionType,
                requestPath,
                requestMethod,
                userId,
                tenantId,
                severity);
        }

        public void SetHttpDetails(
            int? statusCode,
            string? queryString,
            string? requestBody,
            string? userAgent,
            string? ipAddress,
            string? traceId)
        {
            StatusCode = statusCode;
            QueryString = queryString;
            RequestBody = requestBody;
            UserAgent = userAgent;
            IpAddress = ipAddress;
            TraceId = traceId;
        }

        public void SetAdditionalData(string data)
        {
            AdditionalData = data;
        }

        public void Resolve(string resolvedBy, string? notes = null)
        {
            IsResolved = true;
            ResolvedAt = DateTimeOffset.UtcNow;
            ResolvedBy = resolvedBy;
            ResolutionNotes = notes;
        }
    }
}
