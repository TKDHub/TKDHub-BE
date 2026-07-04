namespace Shared.Application.Contracts;

/// <summary>Outcome of validating a file's size/content-type before upload.</summary>
public enum FileValidationResult
{
    Valid = 0,
    Empty,
    TooLarge,
    InvalidType
}
