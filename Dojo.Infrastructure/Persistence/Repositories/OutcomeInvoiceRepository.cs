using Dojo.Domain.Entities;
using Dojo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;
using Shared.Infrastructure.Extensions;

namespace Dojo.Infrastructure.Persistence.Repositories;

internal sealed class OutcomeInvoiceRepository : IOutcomeInvoiceRepository
{
    private readonly DojoDbContext _dbContext;

    public OutcomeInvoiceRepository(DojoDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<OutcomeInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.OutcomeInvoices
            .FirstOrDefaultAsync(
                o => o.Id == id && o.StatusId != (short)EntityStatusEnum.Deleted,
                cancellationToken);

    public async Task<PagedResult<OutcomeInvoice>> GetPagedAsync(
        PagedRequest request,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.OutcomeInvoices
            .Where(o => o.StatusId != (short)EntityStatusEnum.Deleted);

        if (branchId.HasValue)
            query = query.Where(o => o.BranchId == branchId.Value);

        return await query
            .OrderByDescending(o => o.CreatedOn)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<decimal> GetTotalActiveAmountAsync(
        PagedRequest request,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.OutcomeInvoices
            .Where(o => o.StatusId == (short)EntityStatusEnum.Active);

        if (branchId.HasValue)
            query = query.Where(o => o.BranchId == branchId.Value);

        foreach (var filter in request.Filters)
            query = query.ApplyFilter(filter);

        return await query.SumAsync(o => o.Amount, cancellationToken);
    }

    public void Add(OutcomeInvoice invoice)    => _dbContext.OutcomeInvoices.Add(invoice);
    public void Update(OutcomeInvoice invoice) => _dbContext.OutcomeInvoices.Update(invoice);
}
