using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
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

    public UploadOutcomeInvoiceAttachmentCommandHandler(
        IOutcomeInvoiceRepository repository,
        IImageService imageService,
        IUnitOfWork unitOfWork)
    {
        _repository   = repository;
        _imageService = imageService;
        _unitOfWork   = unitOfWork;
    }

    public async Task<Result<string>> Handle(UploadOutcomeInvoiceAttachmentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(request.OutcomeInvoiceId, cancellationToken);
        if (invoice is null)
            return Result.Failure<string>(OutcomeInvoiceErrors.NotFound);

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
            return Result.Failure<string>(attachmentError);

        string attachmentUrl;
        try
        {
            attachmentUrl = await _imageService.UploadAsync(
                request.AttachmentStream,
                request.FileName,
                request.ContentType,
                OutcomeInvoiceAttachmentSettings.Folder,
                cancellationToken);
        }
        catch
        {
            return Result.Failure<string>(OutcomeInvoiceErrors.AttachmentUploadFailed);
        }

        invoice.AttachmentUrl = attachmentUrl;

        _repository.Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(attachmentUrl);
    }
}
