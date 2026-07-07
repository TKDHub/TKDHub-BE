using Identity.Infrastructure.Persistence.Repositories;
using Identity.Infrastructure.Tests.Fixtures;
using Identity.Infrastructure.Tests.TestData;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;

namespace Identity.Infrastructure.Tests.Tenants;

[Collection(PostgresCollection.Name)]
public class TenantRepositoryTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task GetBySubdomainAsync_IsCaseInsensitiveOnStoredLowercaseSubdomain()
    {
        var tenantId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId);
        context.Tenants.Add(EntityFactory.NewTenant(subdomain: "acme"));
        await context.SaveChangesAsync();

        var repository = new TenantRepository(context);

        var found = await repository.GetBySubdomainAsync("ACME");

        Assert.NotNull(found);
    }

    [Fact]
    public async Task ExistsBySubdomainAsync_ReturnsTrueOnlyWhenTaken()
    {
        var tenantId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId);
        context.Tenants.Add(EntityFactory.NewTenant(subdomain: "taken-sub"));
        await context.SaveChangesAsync();

        var repository = new TenantRepository(context);

        Assert.True(await repository.ExistsBySubdomainAsync("taken-sub"));
        Assert.False(await repository.ExistsBySubdomainAsync("free-sub"));
    }

    [Fact]
    public async Task AddingDuplicateSubdomain_ViolatesUniqueConstraintAtDatabaseLevel()
    {
        var tenantId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId);
        context.Tenants.Add(EntityFactory.NewTenant(subdomain: "dupe-sub"));
        await context.SaveChangesAsync();

        context.Tenants.Add(EntityFactory.NewTenant(subdomain: "dupe-sub"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task GetByIdAsync_OnlyIncludesActiveBranches()
    {
        var tenant = EntityFactory.NewTenant();
        // Context's TenantId must match the tenant being created — SaveChangesAsync stamps
        // every Added IHasTenant entity's TenantId to the context's own tenant, so branches
        // must be inserted through a context scoped to this exact tenant.Id.
        await using var context = fixture.CreateContext(tenant.Id);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var activeBranch = EntityFactory.NewBranch(tenant.Id, "Active Branch");
        var deletedBranch = EntityFactory.NewBranch(tenant.Id, "Deleted Branch");
        deletedBranch.StatusId = (short)EntityStatusEnum.Deleted;
        context.Branches.AddRange(activeBranch, deletedBranch);
        await context.SaveChangesAsync();

        // Fresh, untracked context — reusing the seeding context would let EF's change-tracker
        // navigation fixup attach the already-tracked deleted branch too, masking the filtered
        // Include's actual SQL-level result.
        await using var readContext = fixture.CreateContext(tenant.Id);
        var repository = new TenantRepository(readContext);

        var found = await repository.GetByIdAsync(tenant.Id);

        Assert.NotNull(found);
        Assert.Single(found!.Branches);
        Assert.Equal("Active Branch", found.Branches.First().Name);
    }
}
