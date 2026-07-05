using System.ComponentModel.DataAnnotations.Schema;
using Shared.Domain.Enums;

namespace Shared.Domain.Primitives;

public abstract class AuditableEntity<Tkey> : IHasKey<Tkey>, IHasTenant, IAuditable
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
    [Searchable] public required DateTimeOffset CreatedOn { get; set; }
    [Searchable] public DateTimeOffset? ModifiedOn { get; set; }
    [Searchable] public string CreatedByEmail { get; set; }
    [Searchable] public string CreatedByName { get; set; }
    [Searchable] public string? ModifiedByEmail { get; set; }
    [Searchable] public string? ModifiedByName { get; set; }
    [Searchable] public Int16 StatusId { get; set; } = (short)EntityStatusEnum.Active;
}
