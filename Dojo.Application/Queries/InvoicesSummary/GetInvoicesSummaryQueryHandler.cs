using Dojo.Application.Dtos.InvoicesSummary;
using Dojo.Application.Mappings.IncomeInvoices;
using Dojo.Application.Mappings.OutcomeInvoices;
using Dojo.Domain.Repositories;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;
using IncomeInvoiceDto = Dojo.Application.Dtos.IncomeInvoices.IncomeInvoiceDto;
using OutcomeInvoiceDto = Dojo.Application.Dtos.OutcomeInvoices.OutcomeInvoiceDto;

namespace Dojo.Application.Queries.InvoicesSummary;

public sealed record GetInvoicesSummaryQuery(PagedRequest Request) : IQuery<InvoicesSummaryDto>;

/// <summary>
/// Combines income and outcome invoices into one dashboard-style summary: net income
/// collected (Paid minus Refund transactions), total active outcome, and the underlying
/// paged lists (same detail shape as each entity's GetById). Totals aggregate the full
/// filtered set; the lists are paged per the request.
/// </summary>
internal sealed class GetInvoicesSummaryQueryHandler : IQueryHandler<GetInvoicesSummaryQuery, InvoicesSummaryDto>
{
    private readonly IIncomeInvoiceRepository  _incomeRepository;
    private readonly IOutcomeInvoiceRepository _outcomeRepository;
    private readonly IUserContext              _userContext;
    private readonly IBranchContext            _branchContext;

    public GetInvoicesSummaryQueryHandler(
        IIncomeInvoiceRepository incomeRepository,
        IOutcomeInvoiceRepository outcomeRepository,
        IUserContext userContext,
        IBranchContext branchContext)
    {
        _incomeRepository  = incomeRepository;
        _outcomeRepository = outcomeRepository;
        _userContext       = userContext;
        _branchContext     = branchContext;
    }

    public async Task<Result<InvoicesSummaryDto>> Handle(GetInvoicesSummaryQuery request, CancellationToken cancellationToken)
    {
        var branchId = _userContext.IsSuperAdmin ? (Guid?)null : _branchContext.BranchId;

        var totalIncome  = await _incomeRepository.GetTotalNetPaidAsync(request.Request, branchId, cancellationToken);
        var incomePage   = await _incomeRepository.GetPagedAsync(request.Request, branchId, cancellationToken);

        var totalOutcome = await _outcomeRepository.GetTotalActiveAmountAsync(request.Request, branchId, cancellationToken);
        var outcomePage  = await _outcomeRepository.GetPagedAsync(request.Request, branchId, cancellationToken);

        // Currency is snapshotted on every invoice at creation time — read it straight
        // off whatever we already fetched instead of making a separate branch lookup.
        var incomeCurrency  = incomePage.Items.FirstOrDefault()?.Currency  ?? "N/A";
        var outcomeCurrency = outcomePage.Items.FirstOrDefault()?.Currency ?? "N/A";

        var dto = new InvoicesSummaryDto
        {
            TotalIncome     = totalIncome,
            IncomeCurrency  = incomeCurrency,
            TotalOutcome    = totalOutcome,
            OutcomeCurrency = outcomeCurrency,
            IncomeInvoices  = PagedResult<IncomeInvoiceDto>.Create(
                incomePage.Items.ToListDtos(), incomePage.TotalCount, incomePage.Page, incomePage.PageSize),
            OutcomeInvoices = PagedResult<OutcomeInvoiceDto>.Create(
                outcomePage.Items.ToListDtos(), outcomePage.TotalCount, outcomePage.Page, outcomePage.PageSize)
        };

        return Result.Success(dto);
    }
}
