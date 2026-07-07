using Identity.Infrastructure.Persistence.Repositories;
using Identity.Infrastructure.Tests.Fixtures;
using Identity.Infrastructure.Tests.TestData;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Pagination;

namespace Identity.Infrastructure.Tests.Branches;

[Collection(PostgresCollection.Name)]
public class BranchRepositoryTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task ExistsByNameAsync_IsScopedToCallerTenant_ViaGlobalQueryFilter()
    {
        // ExistsByNameAsync has no explicit TenantId predicate — it relies entirely on
        // BaseDbContext's global HasQueryFilter, so this proves that filter actually holds.
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var seedContext = fixture.CreateContext(tenantA))
        {
            // Branches has a real FK to Tenants — a matching Tenant row must exist first.
            seedContext.Tenants.Add(EntityFactory.NewTenant(tenantA));
            seedContext.Branches.Add(EntityFactory.NewBranch(tenantA, "Downtown"));
            await seedContext.SaveChangesAsync();
        }
        await using (var seedContext = fixture.CreateContext(tenantB))
        {
            seedContext.Tenants.Add(EntityFactory.NewTenant(tenantB));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext(tenantB);
        var repository = new BranchRepository(readContext);

        // Same branch name exists, but under a different tenant — must read as free here.
        Assert.False(await repository.ExistsByNameAsync("Downtown", null));
    }

    [Fact]
    public async Task GetPagedAsync_OnlyReturnsCallerTenantsBranches()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var seedContext = fixture.CreateContext(tenantA))
        {
            seedContext.Tenants.Add(EntityFactory.NewTenant(tenantA));
            seedContext.Branches.Add(EntityFactory.NewBranch(tenantA, "Branch A"));
            await seedContext.SaveChangesAsync();
        }
        await using (var seedContext = fixture.CreateContext(tenantB))
        {
            seedContext.Tenants.Add(EntityFactory.NewTenant(tenantB));
            seedContext.Branches.Add(EntityFactory.NewBranch(tenantB, "Branch B"));
            await seedContext.SaveChangesAsync();
        }

        // Fresh context — GetPagedAsync must read what a different request-scoped context sees.
        await using var readContext = fixture.CreateContext(tenantA);
        var repository = new BranchRepository(readContext);

        var page = await repository.GetPagedAsync(new PagedRequest());

        Assert.Single(page.Items);
        Assert.Equal("Branch A", page.Items[0].Name);
    }

    [Fact]
    public async Task AddingDuplicateBranchNameWithinSameTenant_ViolatesUniqueConstraint()
    {
        var tenantId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId);
        context.Tenants.Add(EntityFactory.NewTenant(tenantId));
        context.Branches.Add(EntityFactory.NewBranch(tenantId, "Same Name"));
        await context.SaveChangesAsync();

        context.Branches.Add(EntityFactory.NewBranch(tenantId, "Same Name"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UserBranchManyToMany_RoundTripsThroughJoinTable()
    {
        var tenantId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId);

        context.Tenants.Add(EntityFactory.NewTenant(tenantId));
        var branch = EntityFactory.NewBranch(tenantId);
        var user = EntityFactory.NewUser(tenantId);
        context.Branches.Add(branch);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        branch.Users.Add(user);
        await context.SaveChangesAsync();

        // Fresh, untracked context — avoids EF's change-tracker navigation fixup masking
        // what the query itself actually returns.
        await using var readContext = fixture.CreateContext(tenantId);
        var reloaded = await readContext.Branches
            .Include(b => b.Users)
            .FirstAsync(b => b.Id == branch.Id);

        Assert.Single(reloaded.Users);
        Assert.Equal(user.Id, reloaded.Users.First().Id);
    }
}
