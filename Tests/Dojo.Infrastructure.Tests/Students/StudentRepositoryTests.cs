using Dojo.Domain.Enums;
using Dojo.Infrastructure.Persistence.Repositories;
using Dojo.Infrastructure.Tests.Fixtures;
using Dojo.Infrastructure.Tests.TestData;

namespace Dojo.Infrastructure.Tests.Students;

/// <summary>
/// Proves DeleteStudentCommand's soft delete actually behaves like a delete against real
/// Postgres: once removed, the student must stop being visible to GetByIdAsync, not just
/// carry a different StatusId while still being freely readable/editable.
/// </summary>
[Collection(PostgresCollection.Name)]
public class StudentRepositoryTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task Remove_MarksStudentInactive_AndGetByIdAsyncNoLongerReturnsThem()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        var plan = EntityFactory.NewPlan(tenantId, branchId);
        var student = EntityFactory.NewStudent(tenantId, branchId, plan.Id);
        context.SubscriptionPlans.Add(plan);
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var repository = new StudentRepository(context);

        var found = await repository.GetByIdAsync(student.Id);
        Assert.NotNull(found);

        repository.Remove(found!);
        await context.SaveChangesAsync();

        Assert.Equal((short)StudentStatusEnum.Inactive, found!.StatusId);

        var afterDelete = await repository.GetByIdAsync(student.Id);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task GetPagedAsync_NeverReturnsRemovedStudents()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId, branchId);

        var plan = EntityFactory.NewPlan(tenantId, branchId);
        var active = EntityFactory.NewStudent(tenantId, branchId, plan.Id, "Active");
        var removed = EntityFactory.NewStudent(tenantId, branchId, plan.Id, "Removed");
        context.SubscriptionPlans.Add(plan);
        context.Students.AddRange(active, removed);
        await context.SaveChangesAsync();

        var repository = new StudentRepository(context);
        repository.Remove(removed);
        await context.SaveChangesAsync();

        var page = await repository.GetPagedAsync(new Shared.Domain.Pagination.PagedRequest());

        Assert.Single(page.Items);
        Assert.Equal("Active", page.Items[0].FirstName);
    }

    [Fact]
    public async Task GetExpiredActiveStudentsAsync_ReturnsOnlyActiveStudentsPastTheirEndDate_AcrossAllTenants()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Tenant A: an active student whose membership expired yesterday — should be picked up.
        var tenantA = Guid.NewGuid();
        var branchA = Guid.NewGuid();
        await using (var contextA = fixture.CreateContext(tenantA, branchA))
        {
            var planA = EntityFactory.NewPlan(tenantA, branchA);
            var expiredStudent = EntityFactory.NewStudent(tenantA, branchA, planA.Id, "Expired");
            expiredStudent.EndDate = today.AddDays(-1);
            contextA.SubscriptionPlans.Add(planA);
            contextA.Students.Add(expiredStudent);
            await contextA.SaveChangesAsync();
        }

        // Tenant B: an active student whose membership still has time left — must NOT be picked up.
        var tenantB = Guid.NewGuid();
        var branchB = Guid.NewGuid();
        await using (var contextB = fixture.CreateContext(tenantB, branchB))
        {
            var planB = EntityFactory.NewPlan(tenantB, branchB);
            var stillActive = EntityFactory.NewStudent(tenantB, branchB, planB.Id, "StillActive");
            stillActive.EndDate = today.AddDays(10);
            contextB.SubscriptionPlans.Add(planB);
            contextB.Students.Add(stillActive);
            await contextB.SaveChangesAsync();

            // Also an already-inactive student whose EndDate has passed — must NOT be picked up
            // (the sweep only touches students still marked Active).
            var alreadyInactive = EntityFactory.NewStudent(tenantB, branchB, planB.Id, "AlreadyInactive");
            alreadyInactive.EndDate = today.AddDays(-30);
            alreadyInactive.StatusId = (short)StudentStatusEnum.Inactive;
            contextB.Students.Add(alreadyInactive);
            await contextB.SaveChangesAsync();
        }

        // The sweep itself has no tenant context — a fresh context with an empty tenant proves
        // GetExpiredActiveStudentsAsync doesn't depend on tenant scoping to see across all of them.
        await using var sweepContext = fixture.CreateContext(Guid.Empty, Guid.Empty);
        var repository = new StudentRepository(sweepContext);

        // The container is shared across the whole test collection, so scope the assertion to
        // this test's own two tenants — other tests' leftover data is expected to be present too.
        var expired = (await repository.GetExpiredActiveStudentsAsync(today))
            .Where(s => s.TenantId == tenantA || s.TenantId == tenantB)
            .ToList();

        Assert.Single(expired);
        Assert.Equal("Expired", expired[0].FirstName);
    }
}
