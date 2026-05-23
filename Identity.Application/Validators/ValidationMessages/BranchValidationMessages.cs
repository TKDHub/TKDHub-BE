namespace Identity.Application.Validators.ValidationMessages;

public static class BranchValidationMessages
{
    public const string NameRequired      = "Branch name is required.";
    public const string NameMaxLength     = "Branch name must not exceed 200 characters.";
    public const string EmailRequired     = "Branch email is required.";
    public const string EmailInvalid      = "Branch email must be a valid email address.";
    public const string EmailMaxLength    = "Branch email must not exceed 200 characters.";
    public const string PhoneMaxLength    = "Phone number must not exceed 50 characters.";
    public const string CurrencyMaxLength = "Currency code must not exceed 10 characters.";
    public const string CountryMaxLength  = "Country must not exceed 100 characters.";
    public const string StateMaxLength    = "State must not exceed 100 characters.";
    public const string CityMaxLength     = "City must not exceed 100 characters.";
    public const string StreetMaxLength   = "Street must not exceed 200 characters.";
}
