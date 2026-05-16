using Shared.Domain.Primitives;

namespace Dojo.Domain.Constants;

public static class StudentErrors
{
    public static readonly Error NotFound               = new("Student.NotFound",              "Student not found.");
    public static readonly Error FirstNameRequired      = new("Student.FirstNameRequired",      "First name is required.");
    public static readonly Error LastNameRequired       = new("Student.LastNameRequired",       "Last name is required.");
    public static readonly Error PhoneRequired          = new("Student.PhoneRequired",          "Phone number is required.");
    public static readonly Error EmailAlreadyExists     = new("Student.EmailExists",            "A student with this email already exists.");
    public static readonly Error BranchRequired         = new("Student.BranchRequired",         "Branch is required.");
    public static readonly Error BranchNotFound        = new("Student.BranchNotFound",         "Branch not found.");
    public static readonly Error TenantBranchMismatch  = new("Student.TenantBranchMismatch",   "Branch does not belong to the specified tenant.");
    public static readonly Error SubscriptionRequired   = new("Student.SubscriptionRequired",   "Subscription plan is required.");
    public static readonly Error SubscriptionNotActive  = new("Student.SubscriptionNotActive",  "The selected subscription plan is not active. Please choose an active plan.");
    public static readonly Error NoActivePlans          = new("Student.NoActivePlans",          "No active subscription plans exist for this branch. Please create one before registering students.");
    public static readonly Error ImageUploadFailed      = new("Student.ImageUploadFailed",      "Failed to upload profile image. Please try again.");
    public static readonly Error ImageEmpty            = new("Student.ImageEmpty",             "Image file is empty.");
    public static readonly Error ImageTooLarge         = new("Student.ImageTooLarge",          "Image exceeds the 10 MB limit.");
    public static readonly Error ImageInvalidType      = new("Student.ImageInvalidType",       "Only JPEG, PNG, and WebP images are accepted.");
}
