using Dojo.Infrastructure.Tests.Fixtures;
using Dojo.Infrastructure.Tests.TestData;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Pagination;
using Shared.Infrastructure.Extensions;

namespace Dojo.Infrastructure.Tests.Filtering;

/// <summary>
/// Proves ApplyFilter/ApplySort's [Searchable] allow-list holds when the expression tree is
/// actually translated to SQL by Npgsql, not just evaluated in-memory (Shared.Tests already
/// covers the LINQ-to-Objects case — this closes the "does it really translate" gap).
/// </summary>
[Collection(PostgresCollection.Name)]
public class DynamicQueryTranslationTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task ApplyFilter_OnSearchableColumn_TranslatesToRealSql()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        var plan = EntityFactory.NewPlan(tenantId, branchId);
        context.SubscriptionPlans.Add(plan);
        context.Students.Add(EntityFactory.NewStudent(tenantId, branchId, plan.Id, "Alice"));
        context.Students.Add(EntityFactory.NewStudent(tenantId, branchId, plan.Id, "Bob"));
        await context.SaveChangesAsync();

        var filter = new FilterCriteria { Column = "FirstName", Operator = FilterOperator.Equals, Value = "Alice" };
        var result = await context.Students.ApplyFilter(filter).ToListAsync();

        Assert.Single(result);
        Assert.Equal("Alice", result[0].FirstName);
    }

    [Fact]
    public async Task ApplyFilter_OnNonSearchableColumn_IsSilentlyIgnoredAgainstRealSql()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        var plan = EntityFactory.NewPlan(tenantId, branchId);
        context.SubscriptionPlans.Add(plan);
        var s1 = EntityFactory.NewStudent(tenantId, branchId, plan.Id, "Alice");
        s1.EmergencyContact = "secret-contact-alice";
        var s2 = EntityFactory.NewStudent(tenantId, branchId, plan.Id, "Bob");
        s2.EmergencyContact = "secret-contact-bob";
        context.Students.AddRange(s1, s2);
        await context.SaveChangesAsync();

        // EmergencyContact is deliberately NOT [Searchable] — must be a no-op even against real SQL.
        var filter = new FilterCriteria { Column = "EmergencyContact", Operator = FilterOperator.StartsWith, Value = "secret-contact-a" };
        var result = await context.Students.ApplyFilter(filter).ToListAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ApplySort_OnSearchableColumn_TranslatesToRealSql()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        var plan = EntityFactory.NewPlan(tenantId, branchId);
        context.SubscriptionPlans.Add(plan);
        var cheap = EntityFactory.NewStudent(tenantId, branchId, plan.Id, "Cheap");
        cheap.Price = 10m;
        var expensive = EntityFactory.NewStudent(tenantId, branchId, plan.Id, "Expensive");
        expensive.Price = 500m;
        context.Students.AddRange(cheap, expensive);
        await context.SaveChangesAsync();

        var result = await context.Students.ApplySort("Price", descending: true).ToListAsync();

        Assert.Equal("Expensive", result[0].FirstName);
        Assert.Equal("Cheap", result[1].FirstName);
    }

    [Fact]
    public async Task ApplySort_OnNonSearchableColumn_LeavesQueryUnsortedAgainstRealSql()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        var plan = EntityFactory.NewPlan(tenantId, branchId);
        context.SubscriptionPlans.Add(plan);
        context.Students.Add(EntityFactory.NewStudent(tenantId, branchId, plan.Id, "Alice"));
        context.Students.Add(EntityFactory.NewStudent(tenantId, branchId, plan.Id, "Bob"));
        await context.SaveChangesAsync();

        // ProfileImageUrl is not [Searchable] — sort request must be ignored, not throw.
        var result = await context.Students.ApplySort("ProfileImageUrl", descending: true).ToListAsync();

        Assert.Equal(2, result.Count);
    }
}
