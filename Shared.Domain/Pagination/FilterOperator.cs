namespace Shared.Domain.Pagination
{
    public enum FilterOperator
    {
        Equals = 1,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith
    }
}
