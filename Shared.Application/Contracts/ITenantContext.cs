namespace Shared.Application.Contracts
{
    public interface ITenantContext
    {
        Guid TenantId { get; }
        string TenantName { get; }
        bool IsMultiTenant { get; }
    }
}
