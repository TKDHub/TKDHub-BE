namespace Shared.Domain.Primitives;

/// <summary>
/// Marks an entity property as safe to expose through dynamic filtering and sorting
/// (via <c>PagedRequest.Filters</c> and <c>PagedRequest.SortBy</c>).
///
/// This is an explicit <b>allow-list</b>: any property NOT decorated with this attribute
/// cannot be filtered or sorted on. It prevents clients from probing sensitive columns —
/// password hashes, refresh/reset tokens, logged request bodies, etc. — character-by-character
/// through a boolean (<c>StartsWith</c>/<c>Equals</c>) or ordering oracle. Fail-closed by design:
/// an unmarked or unknown column is silently ignored rather than queried.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SearchableAttribute : Attribute;
