using Dojo.Infrastructure.Persistence.Repositories;
using Dojo.Infrastructure.Tests.Fixtures;
using Dojo.Infrastructure.Tests.TestData;
using Shared.Domain.Pagination;

namespace Dojo.Infrastructure.Tests.TenantIsolation;

/// <summary>
/// Proves the global tenant/branch HasQueryFilter defined in BaseDbContext actually holds when
/// translated to real SQL by Npgsql — a guarantee that is impossible to verify with LINQ-to-Objects
/// since query filters are an EF Core query-compilation feature, not a runtime property.
/// </summary>
[Collection(PostgresCollection.Name)]
public class TenantAndBranchIsolationTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task GetPagedAsync_NeverReturnsInvoicesFromAnotherTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();

        await using (var seedContext = fixture.CreateContext(tenantA, branchA))
        {
            seedContext.OutcomeInvoices.Add(EntityFactory.NewOutcomeInvoice(tenantA, branchA, 10m));
            await seedContext.SaveChangesAsync();
        }
        await using (var seedContext = fixture.CreateContext(tenantB, branchB))
        {
            seedContext.OutcomeInvoices.Add(EntityFactory.NewOutcomeInvoice(tenantB, branchB, 20m));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext(tenantA, branchA);
        var repository = new OutcomeInvoiceRepository(readContext);

        var page = await repository.GetPagedAsync(new PagedRequest());

        Assert.Single(page.Items);
        Assert.All(page.Items, i => Assert.Equal(tenantA, i.TenantId));
    }

    [Fact]
    public async Task GetPagedAsync_BranchAdminOnlySeesOwnBranchWithinSameTenant()
    {
        var tenantId = Guid.NewGuid();
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();

        await using (var seedContext = fixture.CreateContext(tenantId, branchA))
        {
            seedContext.OutcomeInvoices.Add(EntityFactory.NewOutcomeInvoice(tenantId, branchA, 10m));
            await seedContext.SaveChangesAsync();
        }
        await using (var seedContext = fixture.CreateContext(tenantId, branchB))
        {
            seedContext.OutcomeInvoices.Add(EntityFactory.NewOutcomeInvoice(tenantId, branchB, 20m));
            await seedContext.SaveChangesAsync();
        }

        // A branch-scoped context (non-empty BranchId) applies the branch half of the
        // combined tenant+branch query filter automatically, on top of any explicit repo filter.
        await using var readContext = fixture.CreateContext(tenantId, branchA);
        var repository = new OutcomeInvoiceRepository(readContext);

        var page = await repository.GetPagedAsync(new PagedRequest());

        Assert.Single(page.Items);
        Assert.Equal(branchA, page.Items[0].BranchId);
    }

    [Fact]
    public async Task GetPagedAsync_SuperAdminBranchContextEmptyGuid_SeesAllBranchesWithinTenant()
    {
        var tenantId = Guid.NewGuid();
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();

        await using (var seedContext = fixture.CreateContext(tenantId, branchA))
        {
            seedContext.OutcomeInvoices.Add(EntityFactory.NewOutcomeInvoice(tenantId, branchA, 10m));
            await seedContext.SaveChangesAsync();
        }
        await using (var seedContext = fixture.CreateContext(tenantId, branchB))
        {
            seedContext.OutcomeInvoices.Add(EntityFactory.NewOutcomeInvoice(tenantId, branchB, 20m));
            await seedContext.SaveChangesAsync();
        }

        // BaseDbContext treats an empty BranchId as "no branch scoping" (super-admin view).
        await using var readContext = fixture.CreateContext(tenantId, Guid.Empty);
        var repository = new OutcomeInvoiceRepository(readContext);

        var page = await repository.GetPagedAsync(new PagedRequest());

        Assert.Equal(2, page.TotalCount);
    }
}
