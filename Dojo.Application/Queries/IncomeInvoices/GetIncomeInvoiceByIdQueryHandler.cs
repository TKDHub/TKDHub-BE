using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Mappings.IncomeInvoices;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.IncomeInvoices;

public sealed record GetIncomeInvoiceByIdQuery(Guid Id) : IQuery<IncomeInvoiceDto>;

internal sealed class GetIncomeInvoiceByIdQueryHandler : IQueryHandler<GetIncomeInvoiceByIdQuery, IncomeInvoiceDto>
{
    private readonly IIncomeInvoiceRepository _invoiceRepository;
    private readonly ILogger<GetIncomeInvoiceByIdQueryHandler> _logger;

    public GetIncomeInvoiceByIdQueryHandler(IIncomeInvoiceRepository invoiceRepository, ILogger<GetIncomeInvoiceByIdQueryHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _logger             = logger;
    }

    public async Task<Result<IncomeInvoiceDto>> Handle(GetIncomeInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetIncomeInvoiceById: looking up invoice {InvoiceId}", request.Id);

        var invoice = await _invoiceRepository.GetByIdAsync(request.Id, cancellationToken);

        if (invoice is null)
        {
            _logger.LogInformation("GetIncomeInvoiceById: invoice {InvoiceId} not found", request.Id);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.NotFound);
        }

        _logger.LogInformation("GetIncomeInvoiceById: found invoice {InvoiceId}", invoice.Id);
        return Result.Success(invoice.ToDto());
    }
}
