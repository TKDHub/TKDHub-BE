namespace Identity.Application.Validators.ValidationMessages;

public static class UserValidationMessages
{
    public const string UsernameRequired  = "Username is required.";
    public const string UsernameMaxLength = "Username must not exceed 150 characters.";
    public const string EmailInvalid      = "Email must be a valid email address.";
    public const string EmailMaxLength    = "Email must not exceed 150 characters.";
    public const string EmailRequired     = "Email is required.";
    public const string EmailMaxLength100 = "Email must not exceed 100 characters.";
    public const string PhoneMaxLength    = "Phone number must not exceed 50 characters.";
}
