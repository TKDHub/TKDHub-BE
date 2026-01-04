namespace Shared.Domain.Exceptions
{
    public sealed class ValidationException : DomainException
    {
        public ValidationException(Dictionary<string, string[]> errors)
            : base("One or more validation errors occurred")
        {
            Errors = errors;
        }

        public ValidationException(string propertyName, string errorMessage)
            : base($"Validation failed for '{propertyName}': {errorMessage}")
        {
            Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
        }

        public Dictionary<string, string[]> Errors { get; }
    }
}
