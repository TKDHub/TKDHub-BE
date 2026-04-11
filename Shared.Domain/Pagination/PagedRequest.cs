namespace Shared.Domain.Pagination
{
    /// <summary>
    /// Reusable paged + filtered + sorted request.
    /// Use this as the input for any query that returns a list.
    /// </summary>
    public sealed record PagedRequest
    {
        /// <summary>Current page number, 1-based. Defaults to 1.</summary>
        public int Page { get; init; } = 1;

        /// <summary>Number of items per page. Max 100. Defaults to 20.</summary>
        public int PageSize { get; init; } = 20;

        /// <summary>Property name to sort by. Null = no sorting applied.</summary>
        public string? SortBy { get; init; }

        /// <summary>Sort direction. False = ascending (default), True = descending.</summary>
        public bool SortDescending { get; init; } = false;

        /// <summary>
        /// List of dynamic filters. All filters are combined with AND.
        /// Each filter targets a specific column with an operator and value.
        /// </summary>
        public List<FilterCriteria> Filters { get; init; } = [];
    }
}
