using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Domain.Entities
{
    public sealed class Tenant : BaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public DateTime? SubscriptionExpiresAt { get; set; }
        public int MaxUsers { get; set; }
        public int? MaxStorageGB { get; set; }
        public  DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset? ModifiedOn { get; set; }
        public required string CreatedByEmail { get; set; }
        public required string CreatedByName { get; set; }
        public string? ModifiedByEmail { get; set; }
        public string? ModifiedByName { get; set; }
        public required Int16 StatusId { get; set; } = (short)EntityStatusEnum.Active;
    }
}