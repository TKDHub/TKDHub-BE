using Dojo.Domain.Enums;
using Dojo.Infrastructure.Persistence.Repositories;
using Dojo.Infrastructure.Tests.Fixtures;
using Dojo.Infrastructure.Tests.TestData;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;

namespace Dojo.Infrastructure.Tests.IncomeInvoices;

/// <summary>
/// Exercises IncomeInvoiceRepository against a real Postgres instance (Testcontainers), not
/// LINQ-to-Objects — proving the SelectMany(i => i.Transactions).SumAsync(...) refund-netting
/// query actually translates and executes correctly through Npgsql.
/// </summary>
[Collection(PostgresCollection.Name)]
public class IncomeInvoiceRepositoryTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task GetTotalNetPaidAsync_NetsPaidTransactionsMinusRefunds()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        var plan = EntityFactory.NewPlan(tenantId, branchId);
        var student = EntityFactory.NewStudent(tenantId, branchId, plan.Id);
        var invoice = EntityFactory.NewIncomeInvoice(tenantId, branchId, student.Id);

        context.SubscriptionPlans.Add(plan);
        context.Students.Add(student);
        context.IncomeInvoices.Add(invoice);
        context.IncomeTransactions.Add(EntityFactory.NewTransaction(branchId, invoice.Id, 100m, IncomeTransactionStatusEnum.Paid));
        context.IncomeTransactions.Add(EntityFactory.NewTransaction(branchId, invoice.Id, 60m, IncomeTransactionStatusEnum.Paid));
        context.IncomeTransactions.Add(EntityFactory.NewTransaction(branchId, invoice.Id, 30m, IncomeTransactionStatusEnum.Refund));
        await context.SaveChangesAsync();

        var repository = new IncomeInvoiceRepository(context);

        var total = await repository.GetTotalNetPaidAsync(new PagedRequest());

        Assert.Equal(130m, total); // 100 + 60 - 30
    }

    [Fact]
    public async Task GetTotalNetPaidAsync_ScopesToBranchWhenProvided()
    {
        var tenantId = Guid.NewGuid();
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchA);

        var plan = EntityFactory.NewPlan(tenantId, branchA);
        var student = EntityFactory.NewStudent(tenantId, branchA, plan.Id);
        var invoiceA = EntityFactory.NewIncomeInvoice(tenantId, branchA, student.Id);
        var invoiceB = EntityFactory.NewIncomeInvoice(tenantId, branchB, student.Id);

        context.SubscriptionPlans.Add(plan);
        context.Students.Add(student);
        context.IncomeInvoices.AddRange(invoiceA, invoiceB);
        context.IncomeTransactions.Add(EntityFactory.NewTransaction(branchA, invoiceA.Id, 50m));
        context.IncomeTransactions.Add(EntityFactory.NewTransaction(branchB, invoiceB.Id, 999m));
        await context.SaveChangesAsync();

        var repository = new IncomeInvoiceRepository(context);

        var total = await repository.GetTotalNetPaidAsync(new PagedRequest(), branchA);

        Assert.Equal(50m, total);
    }

    [Fact]
    public async Task GetTotalNetPaidAsync_ExcludesDeletedInvoices()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        var plan = EntityFactory.NewPlan(tenantId, branchId);
        var student = EntityFactory.NewStudent(tenantId, branchId, plan.Id);
        var activeInvoice = EntityFactory.NewIncomeInvoice(tenantId, branchId, student.Id);
        var deletedInvoice = EntityFactory.NewIncomeInvoice(tenantId, branchId, student.Id, entityStatus: (short)EntityStatusEnum.Deleted);

        context.SubscriptionPlans.Add(plan);
        context.Students.Add(student);
        context.IncomeInvoices.AddRange(activeInvoice, deletedInvoice);
        context.IncomeTransactions.Add(EntityFactory.NewTransaction(branchId, activeInvoice.Id, 40m));
        context.IncomeTransactions.Add(EntityFactory.NewTransaction(branchId, deletedInvoice.Id, 500m));
        await context.SaveChangesAsync();

        var repository = new IncomeInvoiceRepository(context);

        var total = await repository.GetTotalNetPaidAsync(new PagedRequest());

        Assert.Equal(40m, total);
    }

    [Fact]
    public async Task GetTotalNetPaidAsync_AppliesDynamicFilterOnSearchableColumn()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        var plan = EntityFactory.NewPlan(tenantId, branchId);
        var student = EntityFactory.NewStudent(tenantId, branchId, plan.Id);
        var subscriptionInvoice = EntityFactory.NewIncomeInvoice(tenantId, branchId, student.Id);
        var examInvoice = EntityFactory.NewIncomeInvoice(tenantId, branchId, student.Id);
        examInvoice.Type = IncomeInvoiceTypeEnum.Exam;

        context.SubscriptionPlans.Add(plan);
        context.Students.Add(student);
        context.IncomeInvoices.AddRange(subscriptionInvoice, examInvoice);
        context.IncomeTransactions.Add(EntityFactory.NewTransaction(branchId, subscriptionInvoice.Id, 70m));
        context.IncomeTransactions.Add(EntityFactory.NewTransaction(branchId, examInvoice.Id, 25m));
        await context.SaveChangesAsync();

        var repository = new IncomeInvoiceRepository(context);
        var request = new PagedRequest
        {
            Filters = [new FilterCriteria { Column = "Type", Operator = FilterOperator.Equals, Value = ((short)IncomeInvoiceTypeEnum.Exam).ToString() }]
        };

        var total = await repository.GetTotalNetPaidAsync(request);

        Assert.Equal(25m, total);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsInvoicesOrderedByCreatedOnDescending()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        var plan = EntityFactory.NewPlan(tenantId, branchId);
        var student = EntityFactory.NewStudent(tenantId, branchId, plan.Id);
        context.SubscriptionPlans.Add(plan);
        context.Students.Add(student);
        await context.SaveChangesAsync();

        // SaveChangesAsync stamps CreatedOn = UtcNow on every Added entity, so two sequential
        // saves — not two entities in one save — is what actually produces distinct timestamps.
        var older = EntityFactory.NewIncomeInvoice(tenantId, branchId, student.Id);
        context.IncomeInvoices.Add(older);
        await context.SaveChangesAsync();

        var newer = EntityFactory.NewIncomeInvoice(tenantId, branchId, student.Id);
        context.IncomeInvoices.Add(newer);
        await context.SaveChangesAsync();

        var repository = new IncomeInvoiceRepository(context);

        var page = await repository.GetPagedAsync(new PagedRequest());

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(newer.Id, page.Items[0].Id);
        Assert.Equal(older.Id, page.Items[1].Id);
    }
}
