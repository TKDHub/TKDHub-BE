using Dojo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Dojo.Infrastructure.Tests.Fixtures;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("dojo_integration_tests")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        await using var context = CreateContext(Guid.NewGuid(), Guid.NewGuid());
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// A fresh DojoDbContext scoped to the given tenant/branch, exactly as a real request-scoped
    /// context would be — every query on it goes through the real global tenant+branch query
    /// filters defined in BaseDbContext, against the real Postgres schema from migrations.
    /// </summary>
    public DojoDbContext CreateContext(Guid tenantId, Guid branchId)
    {
        var options = new DbContextOptionsBuilder<DojoDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new DojoDbContext(
            options,
            new FakeTenantContext(tenantId),
            new FakeHttpContextAccessor(),
            new FakeBranchContext(branchId));
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "Postgres";
}
