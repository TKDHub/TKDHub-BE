using Dojo.Domain.Entities;
using Shared.Domain.Pagination;

namespace Dojo.Domain.Repositories;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Student?> GetByIdIgnoringFiltersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Student>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<Student>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, Guid? excludeId, CancellationToken cancellationToken = default);
    void Add(Student student);
    void Update(Student student);
    void Remove(Student student);
}
