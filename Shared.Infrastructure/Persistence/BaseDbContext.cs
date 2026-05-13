using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Contracts;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Shared.Infrastructure.Persistence;

public abstract class BaseDbContext : DbContext
{
    private readonly ITenantContext   _tenantContext;
    private readonly IBranchContext?  _branchContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    protected BaseDbContext(
        DbContextOptions options,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor,
        IBranchContext? branchContext = null) : base(options)
    {
        _tenantContext       = tenantContext;
        _branchContext       = branchContext;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var hasTenant = typeof(IHasTenant).IsAssignableFrom(entityType.ClrType);
            var hasBranch = typeof(IHasBranch).IsAssignableFrom(entityType.ClrType);

            if (hasTenant && hasBranch)
            {
                var method = SetTenantAndBranchQueryMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { modelBuilder });
            }
            else if (hasTenant)
            {
                var method = SetTenantQueryMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    // Tenant-only filter
    private static readonly MethodInfo SetTenantQueryMethod =
        typeof(BaseDbContext)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(m => m.Name == nameof(SetTenantQuery) && m.IsGenericMethodDefinition);

    private void SetTenantQuery<T>(ModelBuilder modelBuilder) where T : class, IHasTenant
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    }

    // Combined tenant + branch filter
    private static readonly MethodInfo SetTenantAndBranchQueryMethod =
        typeof(BaseDbContext)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(m => m.Name == nameof(SetTenantAndBranchQuery) && m.IsGenericMethodDefinition);

    private void SetTenantAndBranchQuery<T>(ModelBuilder modelBuilder)
        where T : class, IHasTenant, IHasBranch
    {
        modelBuilder.Entity<T>().HasQueryFilter(e =>
            e.TenantId == _tenantContext.TenantId &&
            (_branchContext == null || _branchContext.BranchId == Guid.Empty || e.BranchId == _branchContext.BranchId));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userEmail = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;
        var userName  = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not AuditableEntity<Guid> entity) continue;

            if (entry.State == EntityState.Added)
            {
                if (_tenantContext.IsMultiTenant)
                    entity.TenantId = _tenantContext.TenantId;

                // Auto-set BranchId only when not already provided
                if (entity is IHasBranch branchEntity
                    && branchEntity.BranchId == Guid.Empty
                    && _branchContext?.BranchId != null
                    && _branchContext.BranchId != Guid.Empty)
                {
                    branchEntity.BranchId = _branchContext.BranchId;
                }

                entity.CreatedOn      = DateTimeOffset.UtcNow;
                entity.CreatedByName  = !string.IsNullOrEmpty(userName)  ? userName  : "System";
                entity.CreatedByEmail = !string.IsNullOrEmpty(userEmail) ? userEmail : "system@tkdhub.com";

                if (entity.StatusId == 0)
                    entity.StatusId = (short)EntityStatusEnum.Active;
            }

            if (entry.State == EntityState.Modified)
            {
                entity.ModifiedOn      = DateTimeOffset.UtcNow;
                entity.ModifiedByName  = !string.IsNullOrEmpty(userName)  ? userName  : "System";
                entity.ModifiedByEmail = !string.IsNullOrEmpty(userEmail) ? userEmail : "system@tkdhub.com";
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
