using Dojo.Application.Dtos.IncomeInvoices;
using Dojo.Application.Mappings.IncomeInvoices;
using Dojo.Application.Models.IncomeInvoice;
using Dojo.Domain.Constants;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.IncomeInvoices;

public sealed record CreateIncomeInvoiceCommand(CreateIncomeInvoiceModel Model) : ICommand<IncomeInvoiceDto>;

internal sealed class CreateIncomeInvoiceCommandHandler : ICommandHandler<CreateIncomeInvoiceCommand, IncomeInvoiceDto>
{
    private readonly IIncomeInvoiceRepository _invoiceRepository;
    private readonly IStudentRepository       _studentRepository;
    private readonly IBranchService           _branchService;
    private readonly IUnitOfWork              _unitOfWork;
    private readonly ILogger<CreateIncomeInvoiceCommandHandler> _logger;

    public CreateIncomeInvoiceCommandHandler(
        IIncomeInvoiceRepository invoiceRepository,
        IStudentRepository studentRepository,
        IBranchService branchService,
        IUnitOfWork unitOfWork,
        ILogger<CreateIncomeInvoiceCommandHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _studentRepository = studentRepository;
        _branchService     = branchService;
        _unitOfWork        = unitOfWork;
        _logger             = logger;
    }

    public async Task<Result<IncomeInvoiceDto>> Handle(CreateIncomeInvoiceCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;
        _logger.LogInformation("CreateIncomeInvoice: starting for student {StudentId}", model.StudentId);

        if (model.StudentId == Guid.Empty)
        {
            _logger.LogInformation("CreateIncomeInvoice: rejected — student id was empty");
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.StudentRequired);
        }

        if (model.OriginalPrice <= 0)
        {
            _logger.LogInformation("CreateIncomeInvoice: rejected — original price invalid ({Price})", model.OriginalPrice);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.PriceInvalid);
        }

        if (model.DiscountType == DiscountTypeEnum.Percentage && model.DiscountValue is < 0 or > 100)
        {
            _logger.LogInformation("CreateIncomeInvoice: rejected — percentage discount out of range ({Discount})", model.DiscountValue);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.DiscountInvalid);
        }

        if (model.DiscountValue < 0)
        {
            _logger.LogInformation("CreateIncomeInvoice: rejected — negative discount value");
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.DiscountInvalid);
        }

        // Student must exist within the caller's tenant/branch scope (enforced by query filter).
        var student = await _studentRepository.GetByIdAsync(model.StudentId, cancellationToken);
        if (student is null)
        {
            _logger.LogInformation("CreateIncomeInvoice: student {StudentId} not found", model.StudentId);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.StudentNotFound);
        }

        // Currency reflects the branch's current setting, not a possibly-stale student snapshot.
        var branch = await _branchService.GetBranchAsync(student.BranchId, cancellationToken);
        if (branch is null)
        {
            _logger.LogInformation("CreateIncomeInvoice: branch {BranchId} not found", student.BranchId);
            return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.BranchNotFound);
        }

        var invoice = model.ToEntity(student, branch.Currency ?? "N/A");

        // Record the first transaction (if any money was collected). Everything else —
        // payment status and Open/Closed — is derived from the amount against the total.
        var amountPaid = model.AmountPaid ?? 0m;

        if (amountPaid > 0)
        {
            _logger.LogInformation("CreateIncomeInvoice: recording initial payment of {AmountPaid}", amountPaid);
            if (amountPaid > invoice.PriceAfterDiscount)
            {
                _logger.LogInformation("CreateIncomeInvoice: rejected — payment {AmountPaid} exceeds total {Total}", amountPaid, invoice.PriceAfterDiscount);
                return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.PaymentExceedsTotal);
            }

            if (model.PaymentMethod is null)
            {
                _logger.LogInformation("CreateIncomeInvoice: rejected — payment method missing for a paid amount");
                return Result.Failure<IncomeInvoiceDto>(IncomeInvoiceErrors.PaymentMethodRequired);
            }

            invoice.Transactions.Add(model.ToTransaction(invoice, amountPaid));
        }

        invoice.Status = invoice.RemainingAmount <= 0
            ? IncomeInvoiceStatusEnum.Closed
            : IncomeInvoiceStatusEnum.Open;

        _logger.LogInformation("CreateIncomeInvoice: adding invoice entity with status {Status}", invoice.Status);
        _invoiceRepository.Add(invoice);

        _logger.LogInformation("CreateIncomeInvoice: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        invoice.Student = student;
        _logger.LogInformation("CreateIncomeInvoice: succeeded — invoice {InvoiceId} created for student {StudentId}", invoice.Id, student.Id);
        return Result.Success(invoice.ToDto());
    }
}
