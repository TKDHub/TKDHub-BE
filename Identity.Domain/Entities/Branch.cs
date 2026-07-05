using Identity.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Domain.Entities;

public sealed class Branch : AuditableEntity<Guid>
{
    [Searchable] public string Name { get; set; } = string.Empty;
    [Searchable] public string Email { get; set; } = string.Empty;
    [Searchable] public string? PhoneNumber { get; set; }
    [Searchable] public bool Enabled { get; set; }

    [Searchable] public string? AddressCountry { get; set; }
    [Searchable] public string? AddressState { get; set; }
    [Searchable] public string? AddressCity { get; set; }
    [Searchable] public string? AddressStreet { get; set; }

    [Searchable] public DateTimeOffset? TimeZone { get; set; }
    [Searchable] public CurrencyEnum? Currency { get; set; }

    public Tenant? Tenant { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
