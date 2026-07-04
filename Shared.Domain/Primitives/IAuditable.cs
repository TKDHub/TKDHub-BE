namespace Shared.Domain.Primitives;

/// <summary>
/// Marks an entity as carrying the standard audit trail (who/when created and modified).
/// Used by <see cref="Shared.Infrastructure"/>'s DbContext to auto-stamp these fields on
/// every save, independent of whether the entity is also tenant/branch-scoped — this is
/// what lets an entity like Tenant (which must NOT be filtered by its own TenantId) still
/// get the same audit guarantees as every tenant-scoped entity.
/// </summary>
public interface IAuditable
{
    DateTimeOffset  CreatedOn       { get; set; }
    DateTimeOffset? ModifiedOn      { get; set; }
    string          CreatedByEmail  { get; set; }
    string          CreatedByName   { get; set; }
    string?         ModifiedByEmail { get; set; }
    string?         ModifiedByName  { get; set; }
    short           StatusId        { get; set; }
}
