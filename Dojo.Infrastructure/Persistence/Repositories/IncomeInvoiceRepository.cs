using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;
using Shared.Domain.Pagination;
using Shared.Infrastructure.Extensions;

namespace Dojo.Infrastructure.Persistence.Repositories;

internal sealed class IncomeInvoiceRepository : IIncomeInvoiceRepository
{
    private readonly DojoDbContext _dbContext;

    public IncomeInvoiceRepository(DojoDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<IncomeInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.IncomeInvoices
            .Include(i => i.Student)
            .Include(i => i.Transactions)
            .FirstOrDefaultAsync(
                i => i.Id == id && i.StatusId != (short)EntityStatusEnum.Deleted,
                cancellationToken);

    public async Task<PagedResult<IncomeInvoice>> GetPagedAsync(
        PagedRequest request,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.IncomeInvoices
            .Include(i => i.Student)
            .Include(i => i.Transactions)
            .Where(i => i.StatusId != (short)EntityStatusEnum.Deleted);

        if (branchId.HasValue)
            query = query.Where(i => i.BranchId == branchId.Value);

        return await query
            .OrderByDescending(i => i.CreatedOn)
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<decimal> GetTotalNetPaidAsync(
        PagedRequest request,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.IncomeInvoices
            .Where(i => i.StatusId != (short)EntityStatusEnum.Deleted);

        if (branchId.HasValue)
            query = query.Where(i => i.BranchId == branchId.Value);

        foreach (var filter in request.Filters)
            query = query.ApplyFilter(filter);

        return await query
            .SelectMany(i => i.Transactions)
            .SumAsync(t => t.Status == IncomeTransactionStatusEnum.Refund ? -t.Amount : t.Amount, cancellationToken);
    }

    public void Add(IncomeInvoice invoice)    => _dbContext.IncomeInvoices.Add(invoice);
    public void Update(IncomeInvoice invoice) => _dbContext.IncomeInvoices.Update(invoice);
    public void AddTransaction(IncomeTransaction transaction) => _dbContext.IncomeTransactions.Add(transaction);
}
