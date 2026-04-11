using Microsoft.EntityFrameworkCore;
using Shared.Domain.Pagination;
using System.Linq.Expressions;
using System.Reflection;

namespace Shared.Infrastructure.Extensions
{
    public static class QueryableExtensions
    {
        /// <summary>
        /// Applies filters, sorting, and pagination to any IQueryable.
        /// Returns a PagedResult with total count and items.
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            PagedRequest request,
            CancellationToken cancellationToken = default)
        {
            // Apply all filters (AND logic)
            foreach (var filter in request.Filters)
            {
                query = query.ApplyFilter(filter);
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                query = query.ApplySort(request.SortBy, request.SortDescending);
            }

            // Total count before pagination (for TotalPages calculation)
            var totalCount = await query.CountAsync(cancellationToken);

            if (totalCount == 0)
                return PagedResult<T>.Empty(request.Page, request.PageSize);

            // Clamp page and pageSize to valid ranges
            var page = request.Page < 1 ? PaginationDefaults.DefaultPage : request.Page;
            var pageSize = request.PageSize is < 1 or > PaginationDefaults.MaxPageSize
                ? PaginationDefaults.DefaultPageSize
                : request.PageSize;

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<T>.Create(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Applies a single dynamic filter to the query using expression trees.
        /// Supports: =, !=, >, <, >=, <=, Contains, StartsWith, EndsWith.
        /// Column supports dot notation for nested properties: "Address.City".
        /// </summary>
        public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, FilterCriteria filter)
        {
            if (string.IsNullOrWhiteSpace(filter.Column) || string.IsNullOrWhiteSpace(filter.Value))
                return query;

            var parameter = Expression.Parameter(typeof(T), "x");

            // Support nested properties via dot notation e.g. "Address.City"
            Expression property = parameter;
            foreach (var member in filter.Column.Split('.'))
            {
                var propInfo = property.Type.GetProperty(member,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (propInfo is null)
                    return query; // Property does not exist — skip this filter safely

                property = Expression.Property(property, propInfo);
            }

            var propertyType = GetUnderlyingType(((MemberExpression)property).Member);

            // Convert the string value to the actual property type
            object? typedValue;
            try
            {
                typedValue = Convert.ChangeType(filter.Value, propertyType);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
            {
                // Value cannot be converted to property type — skip this filter
                return query;
            }

            var constant = Expression.Constant(typedValue, propertyType);

            Expression comparison = filter.Operator switch
            {
                FilterOperator.Equals              => Expression.Equal(property, constant),
                FilterOperator.NotEquals           => Expression.NotEqual(property, constant),
                FilterOperator.GreaterThan         => Expression.GreaterThan(property, constant),
                FilterOperator.LessThan            => Expression.LessThan(property, constant),
                FilterOperator.GreaterThanOrEqual  => Expression.GreaterThanOrEqual(property, constant),
                FilterOperator.LessThanOrEqual     => Expression.LessThanOrEqual(property, constant),
                FilterOperator.Contains            => BuildStringMethod(property, constant, "Contains"),
                FilterOperator.StartsWith          => BuildStringMethod(property, constant, "StartsWith"),
                FilterOperator.EndsWith            => BuildStringMethod(property, constant, "EndsWith"),
                _                                  => throw new NotSupportedException($"Filter operator '{filter.Operator}' is not supported.")
            };

            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
            return query.Where(lambda);
        }

        /// <summary>
        /// Applies dynamic sorting on any property by name.
        /// </summary>
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string sortBy, bool descending)
        {
            var parameter = Expression.Parameter(typeof(T), "x");

            Expression property = parameter;
            foreach (var member in sortBy.Split('.'))
            {
                property = Expression.Property(property, member);
            }

            var lambda = Expression.Lambda(property, parameter);
            var methodName = descending ? "OrderByDescending" : "OrderBy";

            var result = Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(T), property.Type],
                query.Expression,
                Expression.Quote(lambda));

            return query.Provider.CreateQuery<T>(result);
        }

        // Builds a string method call expression (Contains / StartsWith / EndsWith)
        private static Expression BuildStringMethod(Expression property, Expression constant, string methodName)
        {
            var method = typeof(string).GetMethod(methodName, [typeof(string)])
                ?? throw new InvalidOperationException($"String method '{methodName}' not found.");
            return Expression.Call(property, method, constant);
        }

        // Unwraps Nullable<T> to get the underlying type
        private static Type GetUnderlyingType(MemberInfo member)
        {
            var memberType = member switch
            {
                PropertyInfo p => p.PropertyType,
                FieldInfo f    => f.FieldType,
                _              => throw new ArgumentException("Member must be a property or field.")
            };
            return Nullable.GetUnderlyingType(memberType) ?? memberType;
        }
    }
}
