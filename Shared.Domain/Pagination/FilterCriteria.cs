namespace Shared.Domain.Pagination
{
    /// <summary>
    /// Defines a single filter condition for dynamic querying.
    /// Column supports dot notation for nested properties (e.g. "Address.City").
    /// </summary>
    public sealed record FilterCriteria
    {
        /// <summary>Property name to filter on. Supports dot notation: "Address.City"</summary>
        public string Column { get; init; } = string.Empty;

        /// <summary>Comparison operator: =, !=, >, <, >=, <=, Contains, StartsWith, EndsWith</summary>
        public FilterOperator Operator { get; init; } = FilterOperator.Equals;

        /// <summary>Value to compare against (always provided as string, auto-converted to property type)</summary>
        public string Value { get; init; } = string.Empty;
    }
}
