using Dojo.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Contracts;
using Shared.Domain.Entities;
using Shared.Infrastructure.Persistence;

namespace Dojo.Infrastructure.Persistence;

public sealed class DojoDbContext : BaseDbContext
{
    public DojoDbContext(DbContextOptions<DojoDbContext> options, ITenantContext tenantContext, IHttpContextAccessor httpContextAccessor)
        : base(options, tenantContext, httpContextAccessor)
    {
    }

    public DbSet<Student>  Students  { get; set; }
    public DbSet<ErrorLog> ErrorLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DojoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
