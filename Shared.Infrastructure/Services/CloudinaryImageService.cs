using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Application.Contracts;
using Shared.Infrastructure.Settings;

namespace Shared.Infrastructure.Services;

internal sealed class CloudinaryImageService : IImageService
{
    private readonly Cloudinary                       _cloudinary;
    private readonly ILogger<CloudinaryImageService>  _logger;

    public CloudinaryImageService(
        IOptions<CloudinarySettings>       settings,
        ILogger<CloudinaryImageService>    logger)
    {
        var s = settings.Value;
        _cloudinary = new Cloudinary(new Account(s.CloudName, s.ApiKey, s.ApiSecret))
        {
            Api = { Secure = true }
        };
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream            stream,
        string            fileName,
        string            contentType,
        CancellationToken cancellationToken = default)
    {
        var uploadParams = new ImageUploadParams
        {
            File           = new FileDescription(fileName, stream),
            Folder         = "students",
            UseFilename    = false,
            UniqueFilename = true,
            Overwrite      = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error is not null)
        {
            _logger.LogError("Cloudinary upload failed: {Error}", result.Error.Message);
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
        }

        return result.SecureUrl.ToString();
    }

    public async Task DeleteAsync(string imageId, CancellationToken cancellationToken = default)
    {
        var result = await _cloudinary.DestroyAsync(new DeletionParams(imageId));

        if (result.Error is not null)
            _logger.LogWarning("Cloudinary delete failed for {ImageId}: {Error}", imageId, result.Error.Message);
    }
}
