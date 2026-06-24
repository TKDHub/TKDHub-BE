using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Mappings.IncomeInvoices;
using Dojo.Domain.Repositories;
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

    public GetAllIncomeInvoicesQueryHandler(
        IIncomeInvoiceRepository invoiceRepository,
        IUserContext userContext,
        IBranchContext branchContext)
    {
        _invoiceRepository = invoiceRepository;
        _userContext       = userContext;
        _branchContext     = branchContext;
    }

    public async Task<Result<PagedResult<IncomeInvoiceDto>>> Handle(GetAllIncomeInvoicesQuery request, CancellationToken cancellationToken)
    {
        var branchId = _userContext.IsSuperAdmin ? (Guid?)null : _branchContext.BranchId;

        var result = await _invoiceRepository.GetPagedAsync(
            request.Request,
            branchId,
            cancellationToken);

        return Result.Success(PagedResult<IncomeInvoiceDto>.Create(
            result.Items.ToListDtos(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
