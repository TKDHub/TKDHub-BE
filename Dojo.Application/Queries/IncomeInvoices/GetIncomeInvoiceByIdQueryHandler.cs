using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Mappings.IncomeInvoices;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.IncomeInvoices;

public sealed record GetIncomeInvoiceByIdQuery(Guid Id) : IQuery<IncomeInvoiceDto>;

internal sealed class GetIncomeInvoiceByIdQueryHandler : IQueryHandler<GetIncomeInvoiceByIdQuery, IncomeInvoiceDto>
{
    private readonly IIncomeInvoiceRepository _invoiceRepository;

    public GetIncomeInvoiceByIdQueryHandler(IIncomeInvoiceRepository invoiceRepository)
        => _invoiceRepository = invoiceRepository;

    public async Task<Result<IncomeInvoiceDto>> Handle(GetIncomeInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.NotFound);

        return Result.Success(invoice.ToDto());
    }
}
