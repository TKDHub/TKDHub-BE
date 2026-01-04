using Identity.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Contracts;
using Shared.Domain.Entities;
using Shared.Infrastructure.Persistence;

namespace Identity.Infrastructure.Persistence
{
    public sealed class IdentityDbContext : BaseDbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options, ITenantContext tenantContext, IHttpContextAccessor httpContextAccessor) : base(options, tenantContext, httpContextAccessor)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }

        //public DbSet<UserRole> UserRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}