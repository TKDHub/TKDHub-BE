using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.OutcomeInvoices;

public sealed record UploadOutcomeInvoiceAttachmentCommand(
    Guid   OutcomeInvoiceId,
    Stream AttachmentStream,
    string FileName,
    string ContentType,
    long   Length) : ICommand<string>;

internal sealed class UploadOutcomeInvoiceAttachmentCommandHandler : ICommandHandler<UploadOutcomeInvoiceAttachmentCommand, string>
{
    private readonly IOutcomeInvoiceRepository _repository;
    private readonly IImageService             _imageService;
    private readonly IUnitOfWork                _unitOfWork;
    private readonly ILogger<UploadOutcomeInvoiceAttachmentCommandHandler> _logger;

    public UploadOutcomeInvoiceAttachmentCommandHandler(
        IOutcomeInvoiceRepository repository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<UploadOutcomeInvoiceAttachmentCommandHandler> logger)
    {
        _repository   = repository;
        _imageService = imageService;
        _unitOfWork   = unitOfWork;
        _logger        = logger;
    }

    public async Task<Result<string>> Handle(UploadOutcomeInvoiceAttachmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UploadOutcomeInvoiceAttachment: starting for invoice {InvoiceId}, file {FileName} ({Length} bytes)",
            request.OutcomeInvoiceId, request.FileName, request.Length);

        var invoice = await _repository.GetByIdAsync(request.OutcomeInvoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogInformation("UploadOutcomeInvoiceAttachment: invoice {InvoiceId} not found", request.OutcomeInvoiceId);
            return Result.Failure<string>(OutcomeInvoiceErrors.NotFound);
        }

        _logger.LogInformation("UploadOutcomeInvoiceAttachment: validating file for invoice {InvoiceId}", invoice.Id);
        var validation = _imageService.ValidateFile(
            request.Length, request.ContentType, OutcomeInvoiceAttachmentSettings.MaxBytes, OutcomeInvoiceAttachmentSettings.AllowedContentTypes);

        var attachmentError = validation switch
        {
            FileValidationResult.Empty       => OutcomeInvoiceErrors.AttachmentEmpty,
            FileValidationResult.TooLarge    => OutcomeInvoiceErrors.AttachmentTooLarge,
            FileValidationResult.InvalidType => OutcomeInvoiceErrors.AttachmentInvalidType,
            _ => null
        };

        if (attachmentError is not null)
        {
            _logger.LogInformation("UploadOutcomeInvoiceAttachment: validation failed — {ValidationResult}", validation);
            return Result.Failure<string>(attachmentError);
        }

        string attachmentUrl;
        try
        {
            _logger.LogInformation("UploadOutcomeInvoiceAttachment: uploading to attachment store for invoice {InvoiceId}", invoice.Id);
            attachmentUrl = await _imageService.UploadAsync(
                request.AttachmentStream,
                request.FileName,
                request.ContentType,
                OutcomeInvoiceAttachmentSettings.Folder,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UploadOutcomeInvoiceAttachment: upload failed for invoice {InvoiceId}", invoice.Id);
            return Result.Failure<string>(OutcomeInvoiceErrors.AttachmentUploadFailed);
        }

        invoice.AttachmentUrl = attachmentUrl;

        _repository.Update(invoice);

        _logger.LogInformation("UploadOutcomeInvoiceAttachment: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UploadOutcomeInvoiceAttachment: succeeded — invoice {InvoiceId} attachment updated", invoice.Id);
        return Result.Success(attachmentUrl);
    }
}
