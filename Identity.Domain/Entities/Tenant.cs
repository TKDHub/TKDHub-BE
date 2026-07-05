using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Domain.Entities
{
    public sealed class Tenant : BaseEntity<Guid>, IAuditable
    {
        [Searchable] public string Name { get; set; } = string.Empty;
        [Searchable] public string Subdomain { get; set; } = string.Empty;
        [Searchable] public string ContactEmail { get; set; } = string.Empty;
        [Searchable] public string SubscriptionPlan { get; set; } = string.Empty;
        [Searchable] public DateTime? SubscriptionExpiresAt { get; set; }
        [Searchable] public int MaxUsers { get; set; }
        [Searchable] public int? MaxStorageGB { get; set; }
        [Searchable] public  DateTimeOffset CreatedOn { get; set; }
        [Searchable] public DateTimeOffset? ModifiedOn { get; set; }
        [Searchable] public required string CreatedByEmail { get; set; }
        [Searchable] public required string CreatedByName { get; set; }
        [Searchable] public string? ModifiedByEmail { get; set; }
        [Searchable] public string? ModifiedByName { get; set; }
        [Searchable] public required Int16 StatusId { get; set; } = (short)EntityStatusEnum.Active;

        public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    }
}
