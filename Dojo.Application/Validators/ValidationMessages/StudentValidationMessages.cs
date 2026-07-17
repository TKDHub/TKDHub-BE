namespace Dojo.Application.Validators.ValidationMessages;

public static class StudentValidationMessages
{
    public const string FirstNameRequired    = "First name is required.";
    public const string FirstNameMaxLength   = "First name must not exceed 150 characters.";
    public const string LastNameRequired     = "Last name is required.";
    public const string LastNameMaxLength    = "Last name must not exceed 150 characters.";
    public const string PhoneRequired        = "Phone number is required.";
    public const string PhoneMaxLength       = "Phone number must not exceed 50 characters.";
    public const string EmailInvalid         = "Email must be a valid email address.";
    public const string EmailMaxLength       = "Email must not exceed 150 characters.";
    public const string DateOfBirthPast      = "Date of birth must be in the past.";
    public const string DateOfBirthInvalid   = "Date of birth is not valid.";
    public const string GenderRequired       = "Gender is required.";
    public const string StartDateRequired    = "Start date is required.";
    public const string BeltLevelRequired    = "Belt level is required.";
    public const string PlanRequired         = "Subscription plan is required.";
    public const string PriceInvalid         = "Price must be zero or greater.";
    public const string CurrencyRequired     = "Currency is required.";
    public const string CurrencyMaxLength    = "Currency must not exceed 10 characters.";
    public const string DurationInvalid      = "Duration must be at least 1 month.";
    public const string EmergencyMaxLength   = "Emergency contact must not exceed 200 characters.";
    public const string ImageUrlMaxLength    = "Profile image URL must not exceed 500 characters.";
    public const string StudentIdRequired    = "Student ID is required.";
}
