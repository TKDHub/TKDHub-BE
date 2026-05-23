namespace Identity.Application.Validators.ValidationMessages;

public static class AuthValidationMessages
{
    // Username
    public const string UsernameRequired   = "Username is required.";
    public const string UsernameMaxLength  = "Username must not exceed 150 characters.";

    // Password
    public const string PasswordRequired        = "Password is required.";
    public const string PasswordMinLength       = "Password must be at least 8 characters.";
    public const string PasswordUppercase       = "Password must contain at least one uppercase letter.";
    public const string PasswordLowercase       = "Password must contain at least one lowercase letter.";
    public const string PasswordDigit           = "Password must contain at least one digit.";
    public const string PasswordSpecialChar     = "Password must contain at least one special character.";
    public const string OldPasswordRequired     = "Old password is required.";
    public const string NewPasswordRequired     = "New password is required.";
    public const string ConfirmPasswordRequired = "Confirm password is required.";
    public const string PasswordsMustMatch      = "Passwords do not match.";

    // Identifier
    public const string IdentifierRequired  = "Identifier is required.";
    public const string IdentifierMaxLength = "Identifier must not exceed 200 characters.";

    // OTP
    public const string OtpRequired   = "OTP is required.";
    public const string OtpLength     = "OTP must be 6 digits.";
    public const string OtpDigitsOnly = "OTP must contain only digits.";

    // Refresh token
    public const string RefreshTokenRequired = "Refresh token is required.";
}
