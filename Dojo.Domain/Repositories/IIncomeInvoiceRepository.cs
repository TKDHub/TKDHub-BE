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

    void Add(IncomeInvoice invoice);
    void Update(IncomeInvoice invoice);
    void AddTransaction(IncomeTransaction transaction);
}
