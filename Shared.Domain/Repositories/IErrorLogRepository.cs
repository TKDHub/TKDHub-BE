using Shared.Domain.Entities;
namespace Shared.Domain.Repositories
{
    public interface IErrorLogRepository
    {
        Task<ErrorLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<ErrorLog>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);
        Task<List<ErrorLog>> GetUnresolvedAsync(CancellationToken cancellationToken = default);
        Task<List<ErrorLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
        Task<List<ErrorLog>> GetBySeverityAsync(string severity, CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);
        void Add(ErrorLog errorLog);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
