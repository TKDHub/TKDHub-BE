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
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    protected BaseDbContext(DbContextOptions options, ITenantContext tenantContext, IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global query filter for tenant isolation
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IHasTenant).IsAssignableFrom(entityType.ClrType))
            {
                var method = SetGlobalQueryMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private static readonly MethodInfo SetGlobalQueryMethod =
        typeof(BaseDbContext)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(m => m.Name == nameof(SetGlobalQuery) && m.IsGenericMethodDefinition);

    // ✅ Instance method that captures 'this' context
    private void SetGlobalQuery<T>(ModelBuilder modelBuilder) where T : class, IHasTenant
    {
        // This expression captures 'this._tenantContext' which evaluates dynamically
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userEmail = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;
        var userName = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;
        var entries = ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            if (entry.Entity is AuditableEntity<Guid> auditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    // Set TenantId
                    if (_tenantContext.IsMultiTenant)
                    {
                        auditableEntity.TenantId = _tenantContext.TenantId;
                    }

                    // Set audit fields
                    auditableEntity.CreatedOn = DateTimeOffset.UtcNow;
                    auditableEntity.CreatedByName = !string.IsNullOrEmpty(userName) ? userName : "System";
                    auditableEntity.CreatedByEmail = !string.IsNullOrEmpty(userEmail) ? userEmail : "system@tkdhub.com";

                    if (auditableEntity.StatusId == 0)
                    {
                        auditableEntity.StatusId = (short)EntityStatusEnum.Active;
                    }
                }

                if (entry.State == EntityState.Modified)
                {
                    auditableEntity.ModifiedOn = DateTimeOffset.UtcNow;
                    auditableEntity.ModifiedByName = !string.IsNullOrEmpty(userName) ? userName : "System";
                    auditableEntity.ModifiedByEmail = !string.IsNullOrEmpty(userEmail) ? userEmail : "system@tkdhub.com";
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}