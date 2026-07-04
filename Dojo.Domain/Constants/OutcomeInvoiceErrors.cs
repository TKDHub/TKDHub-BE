using Shared.Domain.Primitives;

namespace Dojo.Domain.Constants;

public static class OutcomeInvoiceErrors
{
    public static readonly Error NotFound            = new("OutcomeInvoice.NotFound",            "Outcome invoice not found.");
    public static readonly Error TitleRequired        = new("OutcomeInvoice.TitleRequired",        "Title is required.");
    public static readonly Error AmountInvalid        = new("OutcomeInvoice.AmountInvalid",        "Amount must be greater than zero.");
    public static readonly Error BranchRequired       = new("OutcomeInvoice.BranchRequired",        "Branch ID is required.");
    public static readonly Error BranchNotFound       = new("OutcomeInvoice.BranchNotFound",        "Branch not found.");
    public static readonly Error TenantBranchMismatch = new("OutcomeInvoice.TenantBranchMismatch", "Branch does not belong to the specified tenant.");

    // ── Attachment ─────────────────────────────────────────────────
    public static readonly Error AttachmentEmpty       = new("OutcomeInvoice.AttachmentEmpty",       "Attachment file is empty.");
    public static readonly Error AttachmentTooLarge    = new("OutcomeInvoice.AttachmentTooLarge",    "Attachment exceeds the maximum allowed size (10 MB).");
    public static readonly Error AttachmentInvalidType = new("OutcomeInvoice.AttachmentInvalidType", "Attachment must be a JPEG, PNG, or WebP image.");
    public static readonly Error AttachmentUploadFailed = new("OutcomeInvoice.AttachmentUploadFailed", "Failed to upload the attachment.");
}
