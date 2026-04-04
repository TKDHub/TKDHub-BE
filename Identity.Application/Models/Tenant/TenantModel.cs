namespace Identity.Application.Models.Tenant
{
    public sealed record TenantModel
    {
        public Guid TenantId { set; get; }
        public string Name { get; init; } = string.Empty;
        public string Subdomain { get; init; } = string.Empty;
        public string ContactEmail { get; init; } = string.Empty;
    }
}
