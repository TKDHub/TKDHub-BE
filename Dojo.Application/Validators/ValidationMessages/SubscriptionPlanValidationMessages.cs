namespace Dojo.Application.Validators.ValidationMessages;

public static class SubscriptionPlanValidationMessages
{
    public const string NameRequired        = "Plan name is required.";
    public const string NameMaxLength       = "Plan name must not exceed 200 characters.";
    public const string DescriptionMaxLength = "Description must not exceed 1000 characters.";
    public const string DurationInvalid     = "Duration must be at least 1 month.";
    public const string PriceInvalid        = "Price must be zero or greater.";
}
