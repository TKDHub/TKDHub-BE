namespace Shared.Application.Models;

/// <summary>
/// Payload sent by downstream services to Identity's POST /api/logs endpoint.
/// </summary>
public sealed record ErrorLogPayload
{
    public Guid   Id             { get; init; }
    public string Message        { get; init; } = string.Empty;
    public string? StackTrace    { get; init; }
    public string? InnerException { get; init; }
    public string ExceptionType  { get; init; } = string.Empty;
    public int?   StatusCode     { get; init; }
    public string? RequestPath   { get; init; }
    public string? RequestMethod { get; init; }
    public string? QueryString   { get; init; }
    public string? RequestBody   { get; init; }
    public string? UserAgent     { get; init; }
    public string? IpAddress     { get; init; }
    public string? UserId        { get; init; }
    public string? TenantId      { get; init; }
    public string? TraceId       { get; init; }
    public string Severity       { get; init; } = "Error";
    public DateTimeOffset Timestamp { get; init; }
    public string? AdditionalData { get; init; }
}
