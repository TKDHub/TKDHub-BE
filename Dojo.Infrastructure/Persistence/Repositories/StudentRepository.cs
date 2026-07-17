using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;
using Shared.Infrastructure.Extensions;

namespace Dojo.Infrastructure.Persistence.Repositories;

internal sealed class StudentRepository : IStudentRepository
{
    private readonly DojoDbContext _dbContext;

    public StudentRepository(DojoDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Students
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.Id == id && s.StatusId != (short)StudentStatusEnum.Inactive, cancellationToken);

    // Same tenant/branch scoping as GetByIdAsync (global query filters still apply — no
    // IgnoreQueryFilters here), but also finds soft-deleted (Inactive) students. Needed by
    // reactivation, which must be able to find the record it's about to restore.
    public async Task<Student?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Students
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<Student?> GetByIdIgnoringFiltersAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Students
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<List<Student>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Students
            .Where(s => s.StatusId == (short)StudentStatusEnum.Active)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);

    public async Task<List<Student>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        => await _dbContext.Students
            .Where(s => ids.Contains(s.Id) && s.StatusId == (short)StudentStatusEnum.Active)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Student>> GetPagedAsync(PagedRequest request, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Students
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.StatusId == (short)StudentStatusEnum.Active);

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId.Value);

        return await query
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<PagedResult<Student>> GetPagedWithClassAsync(PagedRequest request, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Students
            .Include(s => s.Class)
            .Where(s => s.StatusId == (short)StudentStatusEnum.Active);

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId.Value);

        return await query
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToPagedResultAsync(request, cancellationToken);
    }

    // IgnoreQueryFilters: the daily expiry sweep runs outside any HTTP request, so there's no
    // tenant in context to scope by — it must see every tenant/branch in one pass.
    public async Task<List<Student>> GetExpiredActiveStudentsAsync(DateOnly asOf, CancellationToken cancellationToken = default)
        => await _dbContext.Students
            .IgnoreQueryFilters()
            .Where(s => s.StatusId == (short)StudentStatusEnum.Active && s.EndDate < asOf)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId, CancellationToken cancellationToken = default)
        => await _dbContext.Students
            .IgnoreQueryFilters()
            .AnyAsync(
                s => s.Email == email.Trim().ToLowerInvariant()
                  && (excludeId == null || s.Id != excludeId.Value),
                cancellationToken);

    public void Add(Student student)    => _dbContext.Students.Add(student);
    public void Update(Student student) => _dbContext.Students.Update(student);

    // Soft delete — rows are never physically removed (also avoids FK conflicts with invoices).
    // Student uses its own tri-state StudentStatusEnum (Active/Inactive/Frozen), which has no
    // "Deleted" value — EntityStatusEnum.Deleted's numeric value (3) would alias to Frozen here.
    public void Remove(Student student)
    {
        student.StatusId = (short)StudentStatusEnum.Inactive;
        _dbContext.Students.Update(student);
    }
}
