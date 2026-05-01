namespace Identity.Application.Dtos.Branches;

public class BranchDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool Enabled { get; init; }

    public string? AddressCountry { get; init; }
    public string? AddressState { get; init; }
    public string? AddressCity { get; init; }
    public string? AddressStreet { get; init; }

    public DateTimeOffset? TimeZone { get; init; }
}
