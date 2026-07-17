using Dojo.Domain.Entities;
using Dojo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Pagination;
using Shared.Infrastructure.Extensions;

namespace Dojo.Infrastructure.Persistence.Repositories;

internal sealed class StudentActivityLogRepository : IStudentActivityLogRepository
{
    private readonly DojoDbContext _dbContext;

    public StudentActivityLogRepository(DojoDbContext dbContext)
        => _dbContext = dbContext;

    public void Add(StudentActivityLog log) => _dbContext.StudentActivityLogs.Add(log);

    public async Task<PagedResult<StudentActivityLog>> GetPagedByStudentIdAsync(
        Guid studentId,
        PagedRequest request,
        CancellationToken cancellationToken = default)
        => await _dbContext.StudentActivityLogs
            .Where(l => l.StudentId == studentId)
            .OrderByDescending(l => l.CreatedOn)
            .ToPagedResultAsync(request, cancellationToken);
}
