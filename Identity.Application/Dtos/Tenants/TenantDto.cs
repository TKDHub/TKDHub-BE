using Identity.Application.Dtos.Branches;
using Shared.Domain.Enums;

namespace Identity.Application.Dtos.Tenants
{
    public class TenantDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Subdomain { get; init; } = string.Empty;
        public string ContactEmail { get; init; } = string.Empty;
        public EntityStatusEnum Status { get; init; }
        public string SubscriptionPlan { get; init; } = string.Empty;
        public int MaxUsers { get; init; }
        public DateTimeOffset CreatedOn { get; init; }
        public List<BranchDto> Branches { get; init; } = new();
    }
}
