using Dojo.Infrastructure.Persistence.Repositories;
using Dojo.Infrastructure.Tests.Fixtures;
using Dojo.Infrastructure.Tests.TestData;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;

namespace Dojo.Infrastructure.Tests.OutcomeInvoices;

[Collection(PostgresCollection.Name)]
public class OutcomeInvoiceRepositoryTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task GetTotalActiveAmountAsync_SumsOnlyActiveInvoices()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        var active1 = EntityFactory.NewOutcomeInvoice(tenantId, branchId, 40m);
        var active2 = EntityFactory.NewOutcomeInvoice(tenantId, branchId, 60m);
        var inactive = EntityFactory.NewOutcomeInvoice(tenantId, branchId, 999m, entityStatus: (short)EntityStatusEnum.Inactive);
        var deleted = EntityFactory.NewOutcomeInvoice(tenantId, branchId, 500m, entityStatus: (short)EntityStatusEnum.Deleted);

        context.OutcomeInvoices.AddRange(active1, active2, inactive, deleted);
        await context.SaveChangesAsync();

        var repository = new OutcomeInvoiceRepository(context);

        var total = await repository.GetTotalActiveAmountAsync(new PagedRequest());

        Assert.Equal(100m, total);
    }

    [Fact]
    public async Task GetTotalActiveAmountAsync_ScopesToBranchWhenProvided()
    {
        var tenantId = Guid.NewGuid();
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchA);

        context.OutcomeInvoices.Add(EntityFactory.NewOutcomeInvoice(tenantId, branchA, 30m));
        context.OutcomeInvoices.Add(EntityFactory.NewOutcomeInvoice(tenantId, branchB, 999m));
        await context.SaveChangesAsync();

        var repository = new OutcomeInvoiceRepository(context);

        var total = await repository.GetTotalActiveAmountAsync(new PagedRequest(), branchA);

        Assert.Equal(30m, total);
    }

    [Fact]
    public async Task GetTotalActiveAmountAsync_AppliesDynamicFilterOnSearchableColumn()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        context.OutcomeInvoices.Add(EntityFactory.NewOutcomeInvoice(tenantId, branchId, 45m, title: "Rent"));
        context.OutcomeInvoices.Add(EntityFactory.NewOutcomeInvoice(tenantId, branchId, 15m, title: "Utilities"));
        await context.SaveChangesAsync();

        var repository = new OutcomeInvoiceRepository(context);
        var request = new PagedRequest
        {
            Filters = [new FilterCriteria { Column = "Title", Operator = FilterOperator.Equals, Value = "Rent" }]
        };

        var total = await repository.GetTotalActiveAmountAsync(request);

        Assert.Equal(45m, total);
    }

    [Fact]
    public async Task GetTotalActiveAmountAsync_IgnoresFilterOnNonSearchableColumn()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        context.OutcomeInvoices.Add(EntityFactory.NewOutcomeInvoice(tenantId, branchId, 45m));
        context.OutcomeInvoices.Add(EntityFactory.NewOutcomeInvoice(tenantId, branchId, 15m));
        await context.SaveChangesAsync();

        var repository = new OutcomeInvoiceRepository(context);
        // AttachmentUrl is deliberately NOT [Searchable] — the filter must be a silent no-op,
        // proving the allow-list holds even when translated to real SQL, not just LINQ-to-Objects.
        var request = new PagedRequest
        {
            Filters = [new FilterCriteria { Column = "AttachmentUrl", Operator = FilterOperator.StartsWith, Value = "http" }]
        };

        var total = await repository.GetTotalActiveAmountAsync(request);

        Assert.Equal(60m, total);
    }
}
