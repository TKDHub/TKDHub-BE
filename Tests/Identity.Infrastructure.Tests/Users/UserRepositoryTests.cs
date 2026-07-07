using Identity.Infrastructure.Persistence.Repositories;
using Identity.Infrastructure.Tests.Fixtures;
using Identity.Infrastructure.Tests.TestData;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;

namespace Identity.Infrastructure.Tests.Users;

/// <summary>
/// Exercises UserRepository against a real Postgres instance (Testcontainers). Several of its
/// lookups (username/email/phone/refresh-token) deliberately IgnoreQueryFilters — they run
/// before the caller's tenant is known (e.g. during login) — so those must be proven to find
/// users across tenant boundaries by design, while GetByIdAsync must stay tenant-scoped.
/// </summary>
[Collection(PostgresCollection.Name)]
public class UserRepositoryTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task GetByUsernameAsync_FindsUserAcrossTenantBoundary_ByDesign()
    {
        var ownerTenant = Guid.NewGuid();
        var callerTenant = Guid.NewGuid();
        var user = EntityFactory.NewUser(ownerTenant, username: "cross-tenant-user");

        await using (var seedContext = fixture.CreateContext(ownerTenant))
        {
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
        }

        // The caller's own context is scoped to a DIFFERENT tenant, but login lookup must
        // still find the user — IgnoreQueryFilters() is intentional here, not a bug.
        await using var callerContext = fixture.CreateContext(callerTenant);
        var repository = new UserRepository(callerContext);

        var found = await repository.GetByUsernameAsync("cross-tenant-user");

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_IsScopedToCallerTenant_UnlikeUsernameLookup()
    {
        var ownerTenant = Guid.NewGuid();
        var callerTenant = Guid.NewGuid();
        var user = EntityFactory.NewUser(ownerTenant);

        await using (var seedContext = fixture.CreateContext(ownerTenant))
        {
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
        }

        await using var callerContext = fixture.CreateContext(callerTenant);
        var repository = new UserRepository(callerContext);

        var found = await repository.GetByIdAsync(user.Id);

        Assert.Null(found); // the global tenant query filter applies here — no IgnoreQueryFilters
    }

    [Fact]
    public async Task GetByEmailAsync_IsCaseInsensitiveOnStoredLowercaseEmail()
    {
        var tenantId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId);
        context.Users.Add(EntityFactory.NewUser(tenantId, email: "someone@test.com"));
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        var found = await repository.GetByEmailAsync("SOMEONE@TEST.COM");

        Assert.NotNull(found);
    }

    [Fact]
    public async Task GetByRefreshTokenAsync_FindsUserAcrossTenantBoundary()
    {
        var tenantId = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        var user = EntityFactory.NewUser(tenantId);
        user.RefreshToken = "a-real-refresh-token";

        await using (var seedContext = fixture.CreateContext(tenantId))
        {
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext(otherTenant);
        var repository = new UserRepository(readContext);

        var found = await repository.GetByRefreshTokenAsync("a-real-refresh-token");

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }

    [Fact]
    public async Task ExistsByUsernameAsync_ReturnsTrueRegardlessOfTenant()
    {
        var tenantId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId);
        context.Users.Add(EntityFactory.NewUser(tenantId, username: "taken-name"));
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        Assert.True(await repository.ExistsByUsernameAsync("taken-name"));
        Assert.False(await repository.ExistsByUsernameAsync("free-name"));
    }

    [Fact]
    public async Task GetByRoleAsync_TranslatesEnumToStringComparisonCorrectly()
    {
        // Regression guard: RoleId.ToString() inside the LINQ predicate must actually
        // translate to real SQL via Npgsql, not just work by accident under LINQ-to-Objects.
        var tenantId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId);

        var coach = EntityFactory.NewUser(tenantId, username: "coach1");
        var student = EntityFactory.NewUser(tenantId, username: "student1");
        context.Users.AddRange(coach, student);
        await context.SaveChangesAsync();

        context.UserRoles.Add(EntityFactory.NewUserRole(coach.Id, UserRoleEnum.Coach));
        context.UserRoles.Add(EntityFactory.NewUserRole(student.Id, UserRoleEnum.Student));
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        var coaches = await repository.GetByRoleAsync(UserRoleEnum.Coach.ToString());

        Assert.Single(coaches);
        Assert.Equal(coach.Id, coaches[0].Id);
    }

    [Fact]
    public async Task AddingDuplicateUsername_ViolatesUniqueConstraintAtDatabaseLevel()
    {
        var tenantId = Guid.NewGuid();
        await using var context = fixture.CreateContext(tenantId);
        context.Users.Add(EntityFactory.NewUser(tenantId, username: "dupe-name"));
        await context.SaveChangesAsync();

        context.Users.Add(EntityFactory.NewUser(tenantId, username: "dupe-name"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
