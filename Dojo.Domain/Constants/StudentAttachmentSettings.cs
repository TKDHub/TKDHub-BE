namespace Dojo.Domain.Constants;

/// <summary>Upload rules for a student's profile image.</summary>
public static class StudentAttachmentSettings
{
    /// <summary>Cloudinary folder profile images are uploaded under.</summary>
    public const string Folder = "students";

    /// <summary>Maximum accepted profile image size, in bytes (10 MB).</summary>
    public const long MaxBytes = 10 * 1024 * 1024;

    /// <summary>Accepted profile image content types.</summary>
    public static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
}
