using Dojo.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Contracts;
using Shared.Infrastructure.Persistence;

namespace Dojo.Infrastructure.Persistence;

public sealed class DojoDbContext : BaseDbContext
{
    public DojoDbContext(DbContextOptions<DojoDbContext> options, ITenantContext tenantContext, IHttpContextAccessor httpContextAccessor, IBranchContext branchContext)
        : base(options, tenantContext, httpContextAccessor, branchContext)
    {
    }

    public DbSet<Student>            Students            { get; set; }
    public DbSet<SubscriptionPlan>   SubscriptionPlans   { get; set; }
    public DbSet<IncomeInvoice>      IncomeInvoices      { get; set; }
    public DbSet<IncomeTransaction>  IncomeTransactions  { get; set; }
    public DbSet<OutcomeInvoice>     OutcomeInvoices     { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DojoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
