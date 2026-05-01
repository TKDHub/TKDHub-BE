using Shared.Domain.Primitives;

namespace Identity.Domain.Entities;

public sealed class Branch : AuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool Enabled { get; set; }

    public string? AddressCountry { get; set; }
    public string? AddressState { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressStreet { get; set; }

    public DateTimeOffset? TimeZone { get; set; }

    public Tenant? Tenant { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
