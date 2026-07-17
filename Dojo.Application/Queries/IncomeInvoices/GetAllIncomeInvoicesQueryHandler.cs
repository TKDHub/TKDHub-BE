using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Mappings.IncomeInvoices;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.IncomeInvoices;

public sealed record GetAllIncomeInvoicesQuery(PagedRequest Request) : IQuery<PagedResult<IncomeInvoiceDto>>;

internal sealed class GetAllIncomeInvoicesQueryHandler : IQueryHandler<GetAllIncomeInvoicesQuery, PagedResult<IncomeInvoiceDto>>
{
    private readonly IIncomeInvoiceRepository _invoiceRepository;
    private readonly IUserContext             _userContext;
    private readonly IBranchContext           _branchContext;
    private readonly ILogger<GetAllIncomeInvoicesQueryHandler> _logger;

    public GetAllIncomeInvoicesQueryHandler(
        IIncomeInvoiceRepository invoiceRepository,
        IUserContext userContext,
        IBranchContext branchContext,
        ILogger<GetAllIncomeInvoicesQueryHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _userContext       = userContext;
        _branchContext     = branchContext;
        _logger             = logger;
    }

    public async Task<Result<PagedResult<IncomeInvoiceDto>>> Handle(GetAllIncomeInvoicesQuery request, CancellationToken cancellationToken)
    {
        var branchId = _userContext.IsSuperAdmin ? (Guid?)null : _branchContext.BranchId;
        _logger.LogInformation("GetAllIncomeInvoices: querying page {Page} size {PageSize}, branch scope {BranchId}",
            request.Request.Page, request.Request.PageSize, branchId);

        var result = await _invoiceRepository.GetPagedAsync(
            request.Request,
            branchId,
            cancellationToken);

        _logger.LogInformation("GetAllIncomeInvoices: returned {Count} of {Total} invoice(s)", result.Items.Count, result.TotalCount);
        return Result.Success(PagedResult<IncomeInvoiceDto>.Create(
            result.Items.ToListDtos(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
