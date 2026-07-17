using Identity.Application.Commands.Tenants;
using Identity.Application.Models.Tenant;
using Identity.Application.Queries.Tenants;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Domain.Pagination;
using Shared.Domain.Repositories;

namespace Identity.Application.Tests.Tenants;

internal static class TenantTestData
{
    public static Tenant Make(Guid? id = null, string subdomain = "acme", short status = 1) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Acme",
        Subdomain = subdomain,
        ContactEmail = "owner@acme.test",
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "a@a.test",
        CreatedByName = "A",
        StatusId = status
    };
}

public class CreateTenantCommandHandlerTests
{
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateTenantCommandHandler CreateSut() => new(_tenants, _uow, NullLogger<CreateTenantCommandHandler>.Instance);

    private static TenantModel ValidModel() => new()
    {
        Name = "Acme Dojo",
        Subdomain = "acme",
        ContactEmail = "owner@acme.test",
        CreatedByEmail = "admin@acme.test",
        CreatedByName = "Admin"
    };

    [Fact]
    public async Task Handle_WhenNameMissing_ReturnsNameRequired()
    {
        var model = ValidModel() with { Name = "  " };

        var result = await CreateSut().Handle(new CreateTenantCommand(model), default);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantErrors.NameRequired, result.Error);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSubdomainMissing_ReturnsSubdomainRequired()
    {
        var model = ValidModel() with { Subdomain = "" };

        var result = await CreateSut().Handle(new CreateTenantCommand(model), default);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantErrors.SubdomainRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenContactEmailMissing_ReturnsEmailRequired()
    {
        var model = ValidModel() with { ContactEmail = "" };

        var result = await CreateSut().Handle(new CreateTenantCommand(model), default);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantErrors.EmailRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenSubdomainAlreadyExists_ReturnsSubdomainExists()
    {
        _tenants.ExistsBySubdomainAsync("acme", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new CreateTenantCommand(ValidModel()), default);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantErrors.SubdomainExists, result.Error);
        _tenants.DidNotReceive().Add(Arg.Any<Tenant>());
    }

    [Fact]
    public async Task Handle_WhenValid_AddsTenantAndSaves()
    {
        _tenants.ExistsBySubdomainAsync("acme", Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(new CreateTenantCommand(ValidModel()), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Dojo", result.Value.Name);
        _tenants.Received(1).Add(Arg.Is<Tenant>(t => t.Subdomain == "acme"));
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class GetTenantByIdQueryHandlerTests
{
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _tenants.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await new GetTenantByIdQueryHandler(_tenants, NullLogger<GetTenantByIdQueryHandler>.Instance)
            .Handle(new GetTenantByIdQuery(Guid.NewGuid()), default);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsTenantDto()
    {
        var id = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = id,
            Name = "Acme",
            Subdomain = "acme",
            ContactEmail = "owner@acme.test",
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedByEmail = "a@a.test",
            CreatedByName = "A",
            StatusId = 1
        };
        _tenants.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(tenant);

        var result = await new GetTenantByIdQueryHandler(_tenants, NullLogger<GetTenantByIdQueryHandler>.Instance)
            .Handle(new GetTenantByIdQuery(id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal("acme", result.Value.Subdomain);
    }
}

public class UpdateTenantCommandHandlerTests
{
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly NullLogger<UpdateTenantCommandHandler> _log = NullLogger<UpdateTenantCommandHandler>.Instance;

    private UpdateTenantCommandHandler CreateSut() => new(_tenants, _uow, _log);

    private static TenantModel Model(Guid id, string subdomain = "acme") => new()
    {
        TenantId = id,
        Name = "Acme Updated",
        Subdomain = subdomain,
        ContactEmail = "owner@acme.test"
    };

    [Fact]
    public async Task Handle_WhenNameMissing_ReturnsNameRequired()
    {
        var result = await CreateSut().Handle(new UpdateTenantCommand(Model(Guid.NewGuid()) with { Name = "" }), default);
        Assert.Equal(TenantErrors.NameRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ReturnsNotFound()
    {
        _tenants.GetByIdIgnoringFiltersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await CreateSut().Handle(new UpdateTenantCommand(Model(Guid.NewGuid())), default);

        Assert.Equal(TenantErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNewSubdomainTaken_ReturnsSubdomainExists()
    {
        var id = Guid.NewGuid();
        _tenants.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns(TenantTestData.Make(id, subdomain: "old"));
        _tenants.ExistsBySubdomainAsync("new", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new UpdateTenantCommand(Model(id, subdomain: "new")), default);

        Assert.Equal(TenantErrors.SubdomainExists, result.Error);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesAndSaves()
    {
        var id = Guid.NewGuid();
        _tenants.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns(TenantTestData.Make(id, subdomain: "acme"));

        var result = await CreateSut().Handle(new UpdateTenantCommand(Model(id, subdomain: "acme")), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Updated", result.Value.Name);
        _tenants.Received(1).Update(Arg.Any<Tenant>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class DeleteTenantCommandHandlerTests
{
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly NullLogger<DeleteTenantCommandHandler> _log = NullLogger<DeleteTenantCommandHandler>.Instance;

    private DeleteTenantCommandHandler CreateSut() => new(_tenants, _users, _uow, _log);

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _tenants.GetByIdIgnoringFiltersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await CreateSut().Handle(new DeleteTenantCommand(Guid.NewGuid()), default);

        Assert.Equal(TenantErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTenantHasActiveUsers_ReturnsHasActiveUsers()
    {
        var id = Guid.NewGuid();
        _tenants.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns(TenantTestData.Make(id));
        _users.GetByTenantIdAsync(id, Arg.Any<CancellationToken>()).Returns(new List<User> { new() { CreatedOn = DateTimeOffset.UtcNow } });

        var result = await CreateSut().Handle(new DeleteTenantCommand(id), default);

        Assert.Equal(TenantErrors.HasActiveUsers, result.Error);
        _tenants.DidNotReceive().Remove(Arg.Any<Tenant>());
    }

    [Fact]
    public async Task Handle_WhenNoActiveUsers_RemovesAndSaves()
    {
        var id = Guid.NewGuid();
        _tenants.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns(TenantTestData.Make(id));
        _users.GetByTenantIdAsync(id, Arg.Any<CancellationToken>()).Returns(new List<User>());

        var result = await CreateSut().Handle(new DeleteTenantCommand(id), default);

        Assert.True(result.IsSuccess);
        _tenants.Received(1).Remove(Arg.Any<Tenant>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class GetAllTenantsQueryHandlerTests
{
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();

    [Fact]
    public async Task Handle_MapsPagedResult()
    {
        var paged = PagedResult<Tenant>.Create(new List<Tenant> { TenantTestData.Make() }, totalCount: 1, page: 1, pageSize: 20);
        _tenants.GetPagedAsync(Arg.Any<PagedRequest>(), Arg.Any<CancellationToken>()).Returns(paged);

        var result = await new GetAllTenantsQueryHandler(_tenants, NullLogger<GetAllTenantsQueryHandler>.Instance)
            .Handle(new GetAllTenantsQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Single(result.Value.Items);
    }
}

public class GetTenantBySubdomainQueryHandlerTests
{
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _tenants.GetBySubdomainAsync("nope", Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var result = await new GetTenantBySubdomainQueryHandler(_tenants, NullLogger<GetTenantBySubdomainQueryHandler>.Instance)
            .Handle(new GetTenantBySubdomainQuery("nope"), default);

        Assert.Equal(TenantErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsDto()
    {
        _tenants.GetBySubdomainAsync("acme", Arg.Any<CancellationToken>()).Returns(TenantTestData.Make(subdomain: "acme"));

        var result = await new GetTenantBySubdomainQueryHandler(_tenants, NullLogger<GetTenantBySubdomainQueryHandler>.Instance)
            .Handle(new GetTenantBySubdomainQuery("acme"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("acme", result.Value.Subdomain);
    }
}
