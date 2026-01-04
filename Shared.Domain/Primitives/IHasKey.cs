namespace Shared.Domain.Primitives
{
    public interface IHasKey<T>
    {
        T Id { get; set; }
    }
}
