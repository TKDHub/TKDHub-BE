namespace Shared.Application.Contracts;

public interface IImageService
{
    /// <summary>Uploads an image to the CDN and returns the public delivery URL.</summary>
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Deletes an image from the CDN by its Cloudflare image ID.</summary>
    Task DeleteAsync(string imageId, CancellationToken cancellationToken = default);
}
