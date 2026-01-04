using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Domain.Primitives;

public abstract class BaseEntity<Tkey> : IHasKey<Tkey>
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Tkey Id { get; set; }
}