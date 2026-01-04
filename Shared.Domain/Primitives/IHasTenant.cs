namespace Shared.Domain.Primitives
{
    public interface IHasTenant
    {
        Guid TenantId { get; set; }
    }
}
