using Dojo.Domain.Entities;
using Shared.Domain.Pagination;

namespace Dojo.Domain.Repositories;

public interface IOutcomeInvoiceRepository
{
    Task<OutcomeInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<OutcomeInvoice>> GetPagedAsync(
        PagedRequest request,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sum of Amount across Active outcome invoices only (excludes Inactive and Deleted).
    /// Honors the same branch scope and <see cref="PagedRequest.Filters"/> as
    /// <see cref="GetPagedAsync"/>, but ignores paging — it aggregates the full matching set.
    /// </summary>
    Task<decimal> GetTotalActiveAmountAsync(
        PagedRequest request,
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    void Add(OutcomeInvoice invoice);
    void Update(OutcomeInvoice invoice);
}
