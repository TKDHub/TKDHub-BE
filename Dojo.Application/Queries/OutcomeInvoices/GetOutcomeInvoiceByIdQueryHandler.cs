using Dojo.Application.Dtos.OutcomeInvoices;
using Dojo.Application.Mappings.OutcomeInvoices;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.OutcomeInvoices;

public sealed record GetOutcomeInvoiceByIdQuery(Guid Id) : IQuery<OutcomeInvoiceDto>;

internal sealed class GetOutcomeInvoiceByIdQueryHandler : IQueryHandler<GetOutcomeInvoiceByIdQuery, OutcomeInvoiceDto>
{
    private readonly IOutcomeInvoiceRepository _repository;
    private readonly ILogger<GetOutcomeInvoiceByIdQueryHandler> _logger;

    public GetOutcomeInvoiceByIdQueryHandler(IOutcomeInvoiceRepository repository, ILogger<GetOutcomeInvoiceByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger      = logger;
    }

    public async Task<Result<OutcomeInvoiceDto>> Handle(GetOutcomeInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetOutcomeInvoiceById: looking up invoice {InvoiceId}", request.Id);

        var invoice = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (invoice is null)
        {
            _logger.LogInformation("GetOutcomeInvoiceById: invoice {InvoiceId} not found", request.Id);
            return Result.Failure<OutcomeInvoiceDto>(OutcomeInvoiceErrors.NotFound);
        }

        _logger.LogInformation("GetOutcomeInvoiceById: found invoice {InvoiceId}", invoice.Id);
        return Result.Success(invoice.ToDto());
    }
}
