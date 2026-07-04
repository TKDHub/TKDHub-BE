using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Mappings.IncomeInvoices;
using Dojo.Application.Models.IncomeInvoice;
using Dojo.Domain.Constants;
using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.IncomeInvoices;

public sealed record AddIncomeTransactionCommand(AddIncomeTransactionModel Model) : ICommand<IncomeInvoiceDto>;

internal sealed class AddIncomeTransactionCommandHandler : ICommandHandler<AddIncomeTransactionCommand, IncomeInvoiceDto>
{
    private readonly IIncomeInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork              _unitOfWork;

    public AddIncomeTransactionCommandHandler(IIncomeInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork        = unitOfWork;
    }

    public async Task<Result<IncomeInvoiceDto>> Handle(AddIncomeTransactionCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;

        var invoice = await _invoiceRepository.GetByIdAsync(model.IncomeInvoiceId, cancellationToken);
        if (invoice is null)
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.NotFound);

        if (invoice.Status == IncomeInvoiceStatusEnum.Voided)
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.InvoiceVoided);

        if (invoice.Status == IncomeInvoiceStatusEnum.Closed)
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.AlreadyClosed);

        if (model.Amount <= 0 || model.Amount > invoice.RemainingAmount)
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.TransactionAmountInvalid);

        invoice.Transactions.Add(model.ToEntity(invoice));

        // Derive the new lifecycle state — close once fully covered.
        if (invoice.RemainingAmount <= 0)
            invoice.Status = IncomeInvoiceStatusEnum.Closed;

        _invoiceRepository.Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(invoice.ToDto());
    }
}
