using Identity.Domain.Repositories;

namespace Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Unit of Work implementation
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly IdentityDbContext _dbContext;

    public UnitOfWork(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
