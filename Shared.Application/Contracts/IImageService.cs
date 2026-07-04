namespace Shared.Application.Contracts;

public interface IImageService
{
    /// <summary>Uploads an image to the CDN, under the given folder, and returns the public delivery URL.</summary>
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, string folder, CancellationToken cancellationToken = default);

    /// <summary>Deletes an image from the CDN by its Cloudflare image ID.</summary>
    Task DeleteAsync(string imageId, CancellationToken cancellationToken = default);

    /// <summary>Validates a file's size and content type against the given limits.</summary>
    FileValidationResult ValidateFile(long length, string contentType, long maxBytes, IReadOnlyCollection<string> allowedContentTypes);
}
