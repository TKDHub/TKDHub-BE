namespace Identity.Application.Models.Branch;

public sealed record BranchModel
{
    public Guid? BranchId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool Enabled { get; init; }

    public string? AddressCountry { get; init; }
    public string? AddressState { get; init; }
    public string? AddressCity { get; init; }
    public string? AddressStreet { get; init; }

    public DateTimeOffset? TimeZone { get; init; }

    public string CreatedByEmail { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public string? ModifiedByEmail { get; set; }
    public string? ModifiedByName { get; set; }
}
