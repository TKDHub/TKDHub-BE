using Microsoft.EntityFrameworkCore;
using Shared.Domain.Entities;
using Shared.Domain.Pagination;
using Shared.Domain.Repositories;
using Shared.Infrastructure.Extensions;

namespace Shared.Infrastructure.Repositories
{
    public sealed class ErrorLogRepository : IErrorLogRepository
    {
        private readonly DbContext _dbContext;

        public ErrorLogRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ErrorLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<ErrorLog>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        public async Task<PagedResult<ErrorLog>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<ErrorLog>()
                .OrderByDescending(e => e.Timestamp)
                .ToPagedResultAsync(request, cancellationToken);
        }

        public async Task<List<ErrorLog>> GetUnresolvedAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<ErrorLog>()
                .Where(e => !e.IsResolved)
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ErrorLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<ErrorLog>()
                .Where(e => e.Timestamp >= from && e.Timestamp <= to)
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ErrorLog>> GetBySeverityAsync(string severity, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<ErrorLog>()
                .Where(e => e.Severity == severity)
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public void Add(ErrorLog errorLog)
        {
            _dbContext.Set<ErrorLog>().Add(errorLog);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
