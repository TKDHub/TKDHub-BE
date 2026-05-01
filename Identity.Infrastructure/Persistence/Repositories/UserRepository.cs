using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Pagination;
using Shared.Domain.Enums;
using Shared.Infrastructure.Extensions;

namespace Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .Include(u => u.Branches.Where(b => b.StatusId == (short)EntityStatusEnum.Active))
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .Include(u => u.Branches.Where(b => b.StatusId == (short)EntityStatusEnum.Active))
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username == username.Trim(), cancellationToken);
    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Username == username.Trim(), cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .Include(u => u.Branches.Where(b => b.StatusId == (short)EntityStatusEnum.Active))
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .Include(u => u.Branches.Where(b => b.StatusId == (short)EntityStatusEnum.Active))
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PhoneNumber == phone.Trim(), cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .Where(u => u.StatusId == (short)EntityStatusEnum.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<User>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .Where(u => u.StatusId == (short)EntityStatusEnum.Active)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, cancellationToken);
    }

    public async Task<List<User>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId && u.StatusId == (short)EntityStatusEnum.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<User>> GetByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .Where(u => u.StatusId == (short)EntityStatusEnum.Active && u.UserRoles.Any(r => r.RoleId.ToString() == role))
            .ToListAsync(cancellationToken);
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
