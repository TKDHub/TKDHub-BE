using System.ComponentModel.DataAnnotations.Schema;
using Shared.Domain.Enums;

namespace Shared.Domain.Primitives;

public abstract class AuditableEntity<Tkey> : IHasKey<Tkey>, IHasTenant
{
    //protected Entity(Guid id)
    //{
    //    Id = id;
    //}

    //protected Entity()
    //{
    //}
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Tkey Id { get; set; }
    public Guid TenantId { get; set; }
    public required DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
    public string CreatedByEmail { get; set; }
    public string CreatedByName { get; set; }
    public string? ModifiedByEmail { get; set; }
    public string? ModifiedByName { get; set; }
    public Int16 StatusId { get; set; } = (short)EntityStatusEnum.Active;

    //public override int GetHashCode()
    //{
    //    return Id.GetHashCode() * 41;
    //}
}
