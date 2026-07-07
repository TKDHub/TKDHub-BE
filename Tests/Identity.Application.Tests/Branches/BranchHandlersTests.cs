using Identity.Application.Commands.Branches;
using Identity.Application.Models.Branch;
using Identity.Application.Queries.Branches;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Domain.Pagination;
using Shared.Domain.Repositories;

namespace Identity.Application.Tests.Branches;

internal static class BranchTestData
{
    public static Branch Make(Guid? id = null, string name = "Main") => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        Email = "branch@acme.test",
        Enabled = true,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "a@a.test",
        CreatedByName = "A",
        StatusId = 1
    };

    public static BranchModel Model(Guid? id = null, string name = "Main") => new()
    {
        BranchId = id,
        TenantId = Guid.NewGuid(),
        Name = name,
        Email = "branch@acme.test",
        Enabled = true
    };
}

public class CreateBranchCommandHandlerTests
{
    private readonly IBranchRepository _branches = Substitute.For<IBranchRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateBranchCommandHandler CreateSut() => new(_branches, _uow);

    [Fact]
    public async Task Handle_WhenNameMissing_ReturnsNameRequired()
    {
        var result = await CreateSut().Handle(new CreateBranchCommand(BranchTestData.Model() with { Name = " " }), default);
        Assert.Equal(BranchErrors.NameRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenEmailMissing_ReturnsEmailRequired()
    {
        var result = await CreateSut().Handle(new CreateBranchCommand(BranchTestData.Model() with { Email = "" }), default);
        Assert.Equal(BranchErrors.EmailRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNameExists_ReturnsNameAlreadyExists()
    {
        _branches.ExistsByNameAsync("Main", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new CreateBranchCommand(BranchTestData.Model()), default);

        Assert.Equal(BranchErrors.NameAlreadyExists, result.Error);
        _branches.DidNotReceive().Add(Arg.Any<Branch>());
    }

    [Fact]
    public async Task Handle_WhenValid_AddsAndSaves()
    {
        _branches.ExistsByNameAsync("Main", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(new CreateBranchCommand(BranchTestData.Model()), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Main", result.Value.Name);
        _branches.Received(1).Add(Arg.Any<Branch>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class UpdateBranchCommandHandlerTests
{
    private readonly IBranchRepository _branches = Substitute.For<IBranchRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly NullLogger<UpdateBranchCommandHandler> _log = NullLogger<UpdateBranchCommandHandler>.Instance;

    private UpdateBranchCommandHandler CreateSut() => new(_branches, _uow, _log);

    [Fact]
    public async Task Handle_WhenNameMissing_ReturnsNameRequired()
    {
        var result = await CreateSut().Handle(new UpdateBranchCommand(BranchTestData.Model(Guid.NewGuid()) with { Name = "" }), default);
        Assert.Equal(BranchErrors.NameRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _branches.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await CreateSut().Handle(new UpdateBranchCommand(BranchTestData.Model(id)), default);

        Assert.Equal(BranchErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenRenamedToTakenName_ReturnsNameAlreadyExists()
    {
        var id = Guid.NewGuid();
        _branches.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns(BranchTestData.Make(id, name: "Old"));
        _branches.ExistsByNameAsync("New", id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new UpdateBranchCommand(BranchTestData.Model(id, name: "New")), default);

        Assert.Equal(BranchErrors.NameAlreadyExists, result.Error);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesAndSaves()
    {
        var id = Guid.NewGuid();
        _branches.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns(BranchTestData.Make(id, name: "Main"));

        var result = await CreateSut().Handle(new UpdateBranchCommand(BranchTestData.Model(id, name: "Main")), default);

        Assert.True(result.IsSuccess);
        _branches.Received(1).Update(Arg.Any<Branch>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class DeleteBranchCommandHandlerTests
{
    private readonly IBranchRepository _branches = Substitute.For<IBranchRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private DeleteBranchCommandHandler CreateSut() => new(_branches, _uow);

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _branches.GetByIdIgnoringFiltersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await CreateSut().Handle(new DeleteBranchCommand(Guid.NewGuid()), default);

        Assert.Equal(BranchErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenFound_DeactivatesAndSaves()
    {
        var id = Guid.NewGuid();
        var branch = BranchTestData.Make(id);
        _branches.GetByIdIgnoringFiltersAsync(id, Arg.Any<CancellationToken>()).Returns(branch);

        var result = await CreateSut().Handle(new DeleteBranchCommand(id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal((short)Shared.Domain.Enums.EntityStatusEnum.Inactive, branch.StatusId);
        _branches.Received(1).Update(branch);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class GetBranchByIdQueryHandlerTests
{
    private readonly IBranchRepository _branches = Substitute.For<IBranchRepository>();

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _branches.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await new GetBranchByIdQueryHandler(_branches)
            .Handle(new GetBranchByIdQuery(Guid.NewGuid()), default);

        Assert.Equal(BranchErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsDto()
    {
        var id = Guid.NewGuid();
        _branches.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(BranchTestData.Make(id, name: "Main"));

        var result = await new GetBranchByIdQueryHandler(_branches)
            .Handle(new GetBranchByIdQuery(id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Main", result.Value.Name);
    }
}

public class GetAllBranchesQueryHandlerTests
{
    private readonly IBranchRepository _branches = Substitute.For<IBranchRepository>();

    [Fact]
    public async Task Handle_MapsPagedResult()
    {
        var paged = PagedResult<Branch>.Create(new List<Branch> { BranchTestData.Make() }, 1, 1, 20);
        _branches.GetPagedAsync(Arg.Any<PagedRequest>(), Arg.Any<CancellationToken>()).Returns(paged);

        var result = await new GetAllBranchesQueryHandler(_branches)
            .Handle(new GetAllBranchesQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
    }
}
