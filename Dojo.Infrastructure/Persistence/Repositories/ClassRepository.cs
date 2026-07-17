using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;
using Shared.Infrastructure.Extensions;

namespace Dojo.Infrastructure.Persistence.Repositories;

internal sealed class ClassRepository : IClassRepository
{
    private readonly DojoDbContext _dbContext;

    public ClassRepository(DojoDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<Class?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Classes
            .FirstOrDefaultAsync(c => c.Id == id && c.StatusId != (short)EntityStatusEnum.Deleted, cancellationToken);

    public async Task<Class?> GetByIdWithStudentsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Classes
            .Include(c => c.Students.Where(s => s.StatusId == (short)StudentStatusEnum.Active)
                                    .OrderBy(s => s.FirstName).ThenBy(s => s.LastName))
            .FirstOrDefaultAsync(c => c.Id == id && c.StatusId != (short)EntityStatusEnum.Deleted, cancellationToken);

    public async Task<PagedResult<Class>> GetPagedAsync(
        PagedRequest request,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Classes
            .Where(c => c.StatusId != (short)EntityStatusEnum.Deleted);

        if (branchId.HasValue)
            query = query.Where(c => c.BranchId == branchId.Value);

        return await query
            .OrderBy(c => c.Name)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid branchId,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
        => await _dbContext.Classes.AnyAsync(
            c => c.StatusId != (short)EntityStatusEnum.Deleted
              && c.BranchId == branchId
              && c.Name == name.Trim()
              && (excludeId == null || c.Id != excludeId.Value),
            cancellationToken);

    public async Task<bool> HasActiveStudentsAsync(Guid classId, CancellationToken cancellationToken = default)
        => await _dbContext.Students.AnyAsync(
            s => s.ClassId == classId && s.StatusId == (short)StudentStatusEnum.Active,
            cancellationToken);

    public void Add(Class trainingClass)    => _dbContext.Classes.Add(trainingClass);
    public void Update(Class trainingClass) => _dbContext.Classes.Update(trainingClass);
}
