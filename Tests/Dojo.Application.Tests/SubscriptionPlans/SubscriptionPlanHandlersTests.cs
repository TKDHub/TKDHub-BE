using Dojo.Application.Commands.SubscriptionPlans;
using Dojo.Application.Models.SubscriptionPlan;
using Dojo.Application.Queries.SubscriptionPlans;
using Dojo.Domain.Constants;
using Dojo.Domain.Entities;
using Dojo.Domain.Repositories;
using NSubstitute;
using Shared.Application.Contracts;
using Shared.Application.Models;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;
using Shared.Domain.Repositories;

namespace Dojo.Application.Tests.SubscriptionPlans;

internal static class PlanTestData
{
    public static SubscriptionPlan Make(Guid? id = null, Guid? branchId = null, Guid? tenantId = null,
        string name = "Monthly", short status = 1) => new()
    {
        Id = id ?? Guid.NewGuid(),
        BranchId = branchId ?? Guid.NewGuid(),
        TenantId = tenantId ?? Guid.NewGuid(),
        Name = name,
        DurationMonths = 1,
        Price = 50m,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "a@a.test",
        CreatedByName = "A",
        StatusId = status
    };

    public static SubscriptionPlanModel Model(Guid? planId = null, string name = "Monthly", int duration = 1, decimal price = 50m) =>
        new() { PlanId = planId, Name = name, DurationMonths = duration, Price = price };
}

public class CreateSubscriptionPlanCommandHandlerTests
{
    private readonly ISubscriptionPlanRepository _plans = Substitute.For<ISubscriptionPlanRepository>();
    private readonly IBranchService _branchService = Substitute.For<IBranchService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateSubscriptionPlanCommandHandler CreateSut() => new(_plans, _branchService, _uow);

    [Fact]
    public async Task Handle_WhenBranchIdEmpty_ReturnsBranchRequired()
    {
        var result = await CreateSut().Handle(new CreateSubscriptionPlanCommand(PlanTestData.Model(), Guid.Empty, Guid.NewGuid()), default);
        Assert.Equal(SubscriptionPlanErrors.BranchRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNameMissing_ReturnsNameRequired()
    {
        var result = await CreateSut().Handle(
            new CreateSubscriptionPlanCommand(PlanTestData.Model(name: " "), Guid.NewGuid(), Guid.NewGuid()), default);
        Assert.Equal(SubscriptionPlanErrors.NameRequired, result.Error);
    }

    [Fact]
    public async Task Handle_WhenDurationInvalid_ReturnsDurationInvalid()
    {
        var result = await CreateSut().Handle(
            new CreateSubscriptionPlanCommand(PlanTestData.Model(duration: 0), Guid.NewGuid(), Guid.NewGuid()), default);
        Assert.Equal(SubscriptionPlanErrors.DurationInvalid, result.Error);
    }

    [Fact]
    public async Task Handle_WhenPriceNegative_ReturnsPriceInvalid()
    {
        var result = await CreateSut().Handle(
            new CreateSubscriptionPlanCommand(PlanTestData.Model(price: -1), Guid.NewGuid(), Guid.NewGuid()), default);
        Assert.Equal(SubscriptionPlanErrors.PriceInvalid, result.Error);
    }

    [Fact]
    public async Task Handle_WhenBranchNotFound_ReturnsBranchNotFound()
    {
        var branchId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>()).Returns((BranchInfo?)null);

        var result = await CreateSut().Handle(new CreateSubscriptionPlanCommand(PlanTestData.Model(), branchId, Guid.NewGuid()), default);

        Assert.Equal(SubscriptionPlanErrors.BranchNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenTenantMismatch_ReturnsTenantBranchMismatch()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = Guid.NewGuid(), Currency = "JOD", Enabled = true });

        var result = await CreateSut().Handle(new CreateSubscriptionPlanCommand(PlanTestData.Model(), branchId, tenantId), default);

        Assert.Equal(SubscriptionPlanErrors.TenantBranchMismatch, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExistsInBranch_ReturnsNameAlreadyExists()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });
        _plans.ExistsByNameAsync("Monthly", branchId, null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new CreateSubscriptionPlanCommand(PlanTestData.Model(), branchId, tenantId), default);

        Assert.Equal(SubscriptionPlanErrors.NameAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_AddsAndReturnsWithBranchCurrency()
    {
        var branchId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = tenantId, Currency = "JOD", Enabled = true });
        _plans.ExistsByNameAsync("Monthly", branchId, null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(new CreateSubscriptionPlanCommand(PlanTestData.Model(), branchId, tenantId), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("JOD", result.Value.Currency);
        _plans.Received(1).Add(Arg.Any<SubscriptionPlan>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class UpdateSubscriptionPlanCommandHandlerTests
{
    private readonly ISubscriptionPlanRepository _plans = Substitute.For<ISubscriptionPlanRepository>();
    private readonly IBranchService _branchService = Substitute.For<IBranchService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private UpdateSubscriptionPlanCommandHandler CreateSut() => new(_plans, _branchService, _uow);

    [Fact]
    public async Task Handle_WhenPlanIdNull_ReturnsNotFound()
    {
        var result = await CreateSut().Handle(new UpdateSubscriptionPlanCommand(PlanTestData.Model(planId: null)), default);
        Assert.Equal(SubscriptionPlanErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenPlanNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _plans.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((SubscriptionPlan?)null);

        var result = await CreateSut().Handle(new UpdateSubscriptionPlanCommand(PlanTestData.Model(id)), default);

        Assert.Equal(SubscriptionPlanErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenBranchGone_ReturnsBranchNotFound()
    {
        var plan = PlanTestData.Make();
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        _branchService.GetBranchAsync(plan.BranchId, Arg.Any<CancellationToken>()).Returns((BranchInfo?)null);

        var result = await CreateSut().Handle(new UpdateSubscriptionPlanCommand(PlanTestData.Model(plan.Id)), default);

        Assert.Equal(SubscriptionPlanErrors.BranchNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenRenamedToExistingName_ReturnsNameAlreadyExists()
    {
        var plan = PlanTestData.Make(name: "Old");
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        _branchService.GetBranchAsync(plan.BranchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = plan.BranchId, TenantId = plan.TenantId, Currency = "JOD", Enabled = true });
        _plans.ExistsByNameAsync("New", plan.BranchId, plan.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().Handle(new UpdateSubscriptionPlanCommand(PlanTestData.Model(plan.Id, name: "New")), default);

        Assert.Equal(SubscriptionPlanErrors.NameAlreadyExists, result.Error);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesAndSaves()
    {
        var plan = PlanTestData.Make(name: "Old");
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        _branchService.GetBranchAsync(plan.BranchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = plan.BranchId, TenantId = plan.TenantId, Currency = "JOD", Enabled = true });
        _plans.ExistsByNameAsync("New", plan.BranchId, plan.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(new UpdateSubscriptionPlanCommand(PlanTestData.Model(plan.Id, name: "New")), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", plan.Name);
        _plans.Received(1).Update(plan);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class ArchiveSubscriptionPlanCommandHandlerTests
{
    private readonly ISubscriptionPlanRepository _plans = Substitute.For<ISubscriptionPlanRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ArchiveSubscriptionPlanCommandHandler CreateSut() => new(_plans, _uow);

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _plans.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SubscriptionPlan?)null);

        var result = await CreateSut().Handle(new ArchiveSubscriptionPlanCommand(Guid.NewGuid()), default);

        Assert.Equal(SubscriptionPlanErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAlreadyInactive_ReturnsAlreadyArchived()
    {
        var plan = PlanTestData.Make(status: (short)EntityStatusEnum.Inactive);
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await CreateSut().Handle(new ArchiveSubscriptionPlanCommand(plan.Id), default);

        Assert.Equal(SubscriptionPlanErrors.AlreadyArchived, result.Error);
    }

    [Fact]
    public async Task Handle_WhenActive_ArchivesAndSaves()
    {
        var plan = PlanTestData.Make(status: (short)EntityStatusEnum.Active);
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await CreateSut().Handle(new ArchiveSubscriptionPlanCommand(plan.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal((short)EntityStatusEnum.Inactive, plan.StatusId);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class RestoreSubscriptionPlanCommandHandlerTests
{
    private readonly ISubscriptionPlanRepository _plans = Substitute.For<ISubscriptionPlanRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private RestoreSubscriptionPlanCommandHandler CreateSut() => new(_plans, _uow);

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _plans.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SubscriptionPlan?)null);

        var result = await CreateSut().Handle(new RestoreSubscriptionPlanCommand(Guid.NewGuid()), default);

        Assert.Equal(SubscriptionPlanErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WhenAlreadyActive_ReturnsAlreadyActive()
    {
        var plan = PlanTestData.Make(status: (short)EntityStatusEnum.Active);
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await CreateSut().Handle(new RestoreSubscriptionPlanCommand(plan.Id), default);

        Assert.Equal(SubscriptionPlanErrors.AlreadyActive, result.Error);
    }

    [Fact]
    public async Task Handle_WhenInactive_RestoresAndSaves()
    {
        var plan = PlanTestData.Make(status: (short)EntityStatusEnum.Inactive);
        _plans.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var result = await CreateSut().Handle(new RestoreSubscriptionPlanCommand(plan.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal((short)EntityStatusEnum.Active, plan.StatusId);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class SubscriptionPlanQueryHandlersTests
{
    private readonly ISubscriptionPlanRepository _plans = Substitute.For<ISubscriptionPlanRepository>();
    private readonly IBranchService _branchService = Substitute.For<IBranchService>();

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _plans.GetByIdWithStudentsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((SubscriptionPlan?)null);

        var result = await new GetSubscriptionPlanByIdQueryHandler(_plans, _branchService)
            .Handle(new GetSubscriptionPlanByIdQuery(Guid.NewGuid()), default);

        Assert.Equal(SubscriptionPlanErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsDtoWithBranchCurrency()
    {
        var plan = PlanTestData.Make();
        _plans.GetByIdWithStudentsAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        _branchService.GetBranchAsync(plan.BranchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = plan.BranchId, TenantId = plan.TenantId, Currency = "JOD", Enabled = true });

        var result = await new GetSubscriptionPlanByIdQueryHandler(_plans, _branchService)
            .Handle(new GetSubscriptionPlanByIdQuery(plan.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("JOD", result.Value.Currency);
    }

    [Fact]
    public async Task GetAll_WhenSuperAdmin_QueriesAllBranchesButStillReportsCurrentBranchCurrency()
    {
        var branchId = Guid.NewGuid();
        var branchContext = Substitute.For<IBranchContext>();
        var userContext = Substitute.For<IUserContext>();
        branchContext.BranchId.Returns(branchId);
        userContext.IsSuperAdmin.Returns(true);
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = Guid.NewGuid(), Currency = "JOD", Enabled = true });
        var paged = PagedResult<SubscriptionPlan>.Create(new List<SubscriptionPlan> { PlanTestData.Make() }, 1, 1, 20);
        _plans.GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>()).Returns(paged);

        var result = await new GetAllSubscriptionPlansQueryHandler(_plans, branchContext, _branchService, userContext)
            .Handle(new GetAllSubscriptionPlansQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("JOD", result.Value.Items.Single().Currency);
        await _plans.Received(1).GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_WhenBranchAdmin_ScopesToBranch()
    {
        var branchId = Guid.NewGuid();
        var branchContext = Substitute.For<IBranchContext>();
        var userContext = Substitute.For<IUserContext>();
        branchContext.BranchId.Returns(branchId);
        userContext.IsSuperAdmin.Returns(false);
        _branchService.GetBranchAsync(branchId, Arg.Any<CancellationToken>())
            .Returns(new BranchInfo { Id = branchId, TenantId = Guid.NewGuid(), Currency = "JOD", Enabled = true });
        var paged = PagedResult<SubscriptionPlan>.Empty(1, 20);
        _plans.GetPagedAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>()).Returns(paged);

        var result = await new GetAllSubscriptionPlansQueryHandler(_plans, branchContext, _branchService, userContext)
            .Handle(new GetAllSubscriptionPlansQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        await _plans.Received(1).GetPagedAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>());
    }
}
