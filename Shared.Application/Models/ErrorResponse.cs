using System.Text.Json.Serialization;

namespace Shared.Application.Models
{
    public sealed class ErrorResponse
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("status")]
        public int Status { get; init; }

        [JsonPropertyName("detail")]
        public string? Detail { get; init; }

        [JsonPropertyName("instance")]
        public string? Instance { get; init; }

        [JsonPropertyName("traceId")]
        public string? TraceId { get; init; }

        [JsonPropertyName("errors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string[]>? Errors { get; init; }

        public static ErrorResponse Create(
            string type,
            string title,
            int status,
            string? detail = null,
            string? instance = null,
            string? traceId = null,
            Dictionary<string, string[]>? errors = null)
        {
            return new ErrorResponse
            {
                Type = type,
                Title = title,
                Status = status,
                Detail = detail,
                Instance = instance,
                TraceId = traceId,
                Errors = errors
            };
        }
    }
}
