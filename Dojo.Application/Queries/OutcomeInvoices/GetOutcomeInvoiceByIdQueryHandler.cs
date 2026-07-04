using Dojo.Application.Dtos.OutcomeInvoices;
using Dojo.Application.Mappings.OutcomeInvoices;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Queries.OutcomeInvoices;

public sealed record GetOutcomeInvoiceByIdQuery(Guid Id) : IQuery<OutcomeInvoiceDto>;

internal sealed class GetOutcomeInvoiceByIdQueryHandler : IQueryHandler<GetOutcomeInvoiceByIdQuery, OutcomeInvoiceDto>
{
    private readonly IOutcomeInvoiceRepository _repository;

    public GetOutcomeInvoiceByIdQueryHandler(IOutcomeInvoiceRepository repository)
        => _repository = repository;

    public async Task<Result<OutcomeInvoiceDto>> Handle(GetOutcomeInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (invoice is null)
            return Result.Failure<OutcomeInvoiceDto>(OutcomeInvoiceErrors.NotFound);

        return Result.Success(invoice.ToDto());
    }
}
