namespace Identity.Application.Validators.ValidationMessages;

public static class TenantValidationMessages
{
    public const string NameRequired         = "Tenant name is required.";
    public const string NameMaxLength        = "Tenant name must not exceed 200 characters.";
    public const string SubdomainRequired    = "Subdomain is required.";
    public const string SubdomainMaxLength   = "Subdomain must not exceed 50 characters.";
    public const string SubdomainFormat      = "Subdomain can contain only lowercase letters, numbers, and hyphens.";
    public const string ContactEmailRequired = "Contact email is required.";
    public const string ContactEmailInvalid  = "Contact email must be a valid email address.";
    public const string ContactEmailMaxLength = "Contact email must not exceed 256 characters.";
}
