using Shared.Domain.Primitives;

namespace Dojo.Domain.Constants;

public static class SubscriptionPlanErrors
{
    public static readonly Error NotFound           = new("SubscriptionPlan.NotFound",          "Subscription plan not found.");
    public static readonly Error NameRequired       = new("SubscriptionPlan.NameRequired",       "Plan name is required.");
    public static readonly Error NameAlreadyExists  = new("SubscriptionPlan.NameExists",         "A subscription plan with this name already exists in the branch.");
    public static readonly Error DurationInvalid    = new("SubscriptionPlan.DurationInvalid",    "Duration must be at least 1 month.");
    public static readonly Error PriceInvalid       = new("SubscriptionPlan.PriceInvalid",       "Price must be greater than or equal to zero.");
    public static readonly Error BranchRequired      = new("SubscriptionPlan.BranchRequired",      "Branch ID is required.");
    public static readonly Error BranchNotFound     = new("SubscriptionPlan.BranchNotFound",     "Branch not found.");
    public static readonly Error TenantBranchMismatch = new("SubscriptionPlan.TenantBranchMismatch", "Branch does not belong to the specified tenant.");
    public static readonly Error AlreadyArchived    = new("SubscriptionPlan.AlreadyArchived",    "Subscription plan is already archived.");
    public static readonly Error AlreadyActive      = new("SubscriptionPlan.AlreadyActive",      "Subscription plan is already active.");
}
