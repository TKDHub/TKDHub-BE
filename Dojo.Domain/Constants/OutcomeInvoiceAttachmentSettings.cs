namespace Dojo.Domain.Constants;

/// <summary>Upload rules for an outcome invoice's receipt/attachment.</summary>
public static class OutcomeInvoiceAttachmentSettings
{
    /// <summary>Cloudinary folder receipt/attachment files are uploaded under.</summary>
    public const string Folder = "outcome-invoices";

    /// <summary>Maximum accepted attachment size, in bytes (10 MB).</summary>
    public const long MaxBytes = 10 * 1024 * 1024;

    /// <summary>Accepted attachment content types.</summary>
    public static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
}
