using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;

namespace Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// User repository implementation
/// </summary>
internal sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.Where(u => u.StatusId == (short)EntityStatusEnum.Active).ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, cancellationToken);
    }

    public async Task<List<User>> GetByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.Where(u => u.StatusId == (short)EntityStatusEnum.Active && u.Roles.Contains(role)).ToListAsync(cancellationToken);
    }

    public void Add(User user)
    {
        _dbContext.Users.Add(user);
    }

    public void Update(User user)
    {
        _dbContext.Users.Update(user);
    }

    public void Remove(User user)
    {
        _dbContext.Users.Remove(user);
    }
}
