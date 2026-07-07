using Dojo.Application.Queries.InvoicesSummary;
using Dojo.Application.Tests.IncomeInvoices;
using Dojo.Application.Tests.OutcomeInvoices;
using Dojo.Domain.Repositories;
using NSubstitute;
using Shared.Application.Contracts;
using Shared.Domain.Pagination;
using Shared.Domain.Repositories;

namespace Dojo.Application.Tests.InvoicesSummary;

public class GetInvoicesSummaryQueryHandlerTests
{
    private readonly IIncomeInvoiceRepository _income = Substitute.For<IIncomeInvoiceRepository>();
    private readonly IOutcomeInvoiceRepository _outcome = Substitute.For<IOutcomeInvoiceRepository>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IBranchContext _branchContext = Substitute.For<IBranchContext>();

    private GetInvoicesSummaryQueryHandler CreateSut() => new(_income, _outcome, _userContext, _branchContext);

    [Fact]
    public async Task Handle_WhenSuperAdmin_QueriesBothRepositoriesAcrossAllBranches()
    {
        _userContext.IsSuperAdmin.Returns(true);
        _income.GetTotalNetPaidAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>()).Returns(500m);
        _income.GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(PagedResult<Dojo.Domain.Entities.IncomeInvoice>.Empty(1, 20));
        _outcome.GetTotalActiveAmountAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>()).Returns(200m);
        _outcome.GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(PagedResult<Dojo.Domain.Entities.OutcomeInvoice>.Empty(1, 20));

        var result = await CreateSut().Handle(new GetInvoicesSummaryQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(500m, result.Value.TotalIncome);
        Assert.Equal(200m, result.Value.TotalOutcome);
        await _income.Received(1).GetTotalNetPaidAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>());
        await _outcome.Received(1).GetTotalActiveAmountAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBranchAdmin_ScopesBothRepositoriesToBranch()
    {
        var branchId = Guid.NewGuid();
        _userContext.IsSuperAdmin.Returns(false);
        _branchContext.BranchId.Returns(branchId);
        _income.GetTotalNetPaidAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>()).Returns(0m);
        _income.GetPagedAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>())
            .Returns(PagedResult<Dojo.Domain.Entities.IncomeInvoice>.Empty(1, 20));
        _outcome.GetTotalActiveAmountAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>()).Returns(0m);
        _outcome.GetPagedAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>())
            .Returns(PagedResult<Dojo.Domain.Entities.OutcomeInvoice>.Empty(1, 20));

        var result = await CreateSut().Handle(new GetInvoicesSummaryQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        await _income.Received(1).GetPagedAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>());
        await _outcome.Received(1).GetPagedAsync(Arg.Any<PagedRequest>(), branchId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenInvoicesExist_DerivesCurrencyFromFirstItemOfEachList()
    {
        _userContext.IsSuperAdmin.Returns(true);
        var incomeInvoice = InvoiceTestData.MakeInvoice();
        var outcomeInvoice = OutcomeInvoiceTestData.Make();

        _income.GetTotalNetPaidAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>()).Returns(100m);
        _income.GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(PagedResult<Dojo.Domain.Entities.IncomeInvoice>.Create(new List<Dojo.Domain.Entities.IncomeInvoice> { incomeInvoice }, 1, 1, 20));
        _outcome.GetTotalActiveAmountAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>()).Returns(50m);
        _outcome.GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(PagedResult<Dojo.Domain.Entities.OutcomeInvoice>.Create(new List<Dojo.Domain.Entities.OutcomeInvoice> { outcomeInvoice }, 1, 1, 20));

        var result = await CreateSut().Handle(new GetInvoicesSummaryQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("JOD", result.Value.IncomeCurrency);
        Assert.Equal("JOD", result.Value.OutcomeCurrency);
        Assert.Single(result.Value.IncomeInvoices.Items);
        Assert.Single(result.Value.OutcomeInvoices.Items);
    }

    [Fact]
    public async Task Handle_WhenNoInvoicesExist_DefaultsCurrencyToNA()
    {
        _userContext.IsSuperAdmin.Returns(true);
        _income.GetTotalNetPaidAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>()).Returns(0m);
        _income.GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(PagedResult<Dojo.Domain.Entities.IncomeInvoice>.Empty(1, 20));
        _outcome.GetTotalActiveAmountAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>()).Returns(0m);
        _outcome.GetPagedAsync(Arg.Any<PagedRequest>(), null, Arg.Any<CancellationToken>())
            .Returns(PagedResult<Dojo.Domain.Entities.OutcomeInvoice>.Empty(1, 20));

        var result = await CreateSut().Handle(new GetInvoicesSummaryQuery(new PagedRequest()), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("N/A", result.Value.IncomeCurrency);
        Assert.Equal("N/A", result.Value.OutcomeCurrency);
    }
}
