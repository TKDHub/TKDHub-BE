using Dojo.Application.Dtos.OutcomeInvoices;
using Dojo.Application.Mappings.OutcomeInvoices;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.OutcomeInvoices;

public sealed record GetAllOutcomeInvoicesQuery(PagedRequest Request) : IQuery<PagedResult<OutcomeInvoiceDto>>;

internal sealed class GetAllOutcomeInvoicesQueryHandler : IQueryHandler<GetAllOutcomeInvoicesQuery, PagedResult<OutcomeInvoiceDto>>
{
    private readonly IOutcomeInvoiceRepository _repository;
    private readonly IUserContext              _userContext;
    private readonly IBranchContext            _branchContext;
    private readonly ILogger<GetAllOutcomeInvoicesQueryHandler> _logger;

    public GetAllOutcomeInvoicesQueryHandler(
        IOutcomeInvoiceRepository repository,
        IUserContext userContext,
        IBranchContext branchContext,
        ILogger<GetAllOutcomeInvoicesQueryHandler> logger)
    {
        _repository    = repository;
        _userContext   = userContext;
        _branchContext = branchContext;
        _logger         = logger;
    }

    public async Task<Result<PagedResult<OutcomeInvoiceDto>>> Handle(GetAllOutcomeInvoicesQuery request, CancellationToken cancellationToken)
    {
        var branchId = _userContext.IsSuperAdmin ? (Guid?)null : _branchContext.BranchId;
        _logger.LogInformation("GetAllOutcomeInvoices: querying page {Page} size {PageSize}, branch scope {BranchId}",
            request.Request.Page, request.Request.PageSize, branchId);

        var result = await _repository.GetPagedAsync(request.Request, branchId, cancellationToken);

        _logger.LogInformation("GetAllOutcomeInvoices: returned {Count} of {Total} invoice(s)", result.Items.Count, result.TotalCount);
        return Result.Success(PagedResult<OutcomeInvoiceDto>.Create(
            result.Items.ToListDtos(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }
}
