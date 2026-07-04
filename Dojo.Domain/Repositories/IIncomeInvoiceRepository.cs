using Dojo.Domain.Entities;
using Shared.Domain.Pagination;

namespace Dojo.Domain.Repositories;

public interface IIncomeInvoiceRepository
{
    Task<IncomeInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<IncomeInvoice>> GetPagedAsync(
        PagedRequest request,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Net collected across matching invoices: sum of Paid transactions minus sum of
    /// Refund transactions. Honors the same branch scope and <see cref="PagedRequest.Filters"/>
    /// as <see cref="GetPagedAsync"/>, but ignores paging — it aggregates the full matching set.
    /// </summary>
    Task<decimal> GetTotalNetPaidAsync(
        PagedRequest request,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    void Add(IncomeInvoice invoice);
    void Update(IncomeInvoice invoice);
    void AddTransaction(IncomeTransaction transaction);
}
