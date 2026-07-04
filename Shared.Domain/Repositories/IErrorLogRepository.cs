using Shared.Domain.Entities;
using Shared.Domain.Pagination;
namespace Shared.Domain.Repositories
{
    public interface IErrorLogRepository
    {
        Task<ErrorLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<PagedResult<ErrorLog>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
        Task<List<ErrorLog>> GetUnresolvedAsync(CancellationToken cancellationToken = default);
        Task<List<ErrorLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
        Task<List<ErrorLog>> GetBySeverityAsync(string severity, CancellationToken cancellationToken = default);
        void Add(ErrorLog errorLog);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
