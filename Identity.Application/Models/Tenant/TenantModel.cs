namespace Identity.Application.Models.Tenant
{
    public sealed record TenantModel
    {
        public Guid TenantId { get; set; }
        public string Name { get; init; } = string.Empty;
        public string Subdomain { get; init; } = string.Empty;
        public string ContactEmail { get; init; } = string.Empty;

        // Set by the controller from JWT claims — tracks who created/modified the tenant
        public string CreatedByEmail { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
    }
}
