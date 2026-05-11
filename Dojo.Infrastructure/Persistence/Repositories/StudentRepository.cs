using Dojo.Domain.Entities;
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
            .FirstOrDefaultAsync(s => s.Id == id && s.StatusId == (short)EntityStatusEnum.Active, cancellationToken);

    public async Task<Student?> GetByIdIgnoringFiltersAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Students
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id && s.StatusId == (short)EntityStatusEnum.Active, cancellationToken);

    public async Task<List<Student>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Students
            .Where(s => s.StatusId == (short)EntityStatusEnum.Active)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Student>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
        => await _dbContext.Students
            .Where(s => s.StatusId == (short)EntityStatusEnum.Active)
            .ToPagedResultAsync(request, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId, CancellationToken cancellationToken = default)
        => await _dbContext.Students.AnyAsync(
            s => s.StatusId == (short)EntityStatusEnum.Active
              && s.Email == email.Trim().ToLowerInvariant()
              && (excludeId == null || s.Id != excludeId.Value),
            cancellationToken);

    public void Add(Student student)    => _dbContext.Students.Add(student);
    public void Update(Student student) => _dbContext.Students.Update(student);
    public void Remove(Student student) => _dbContext.Students.Remove(student);
}
