using Dojo.Application.Mappings.Students;
using Dojo.Domain.Constants;
using Dojo.Domain.Enums;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Students;

public sealed record UploadStudentImageCommand(
    Guid   StudentId,
    Stream ImageStream,
    string FileName,
    string ContentType,
    long   Length,
    string UploadedByEmail,
    string UploadedByName) : ICommand<string>;

internal sealed class UploadStudentImageCommandHandler : ICommandHandler<UploadStudentImageCommand, string>
{
    private readonly IStudentRepository            _studentRepository;
    private readonly IImageService                 _imageService;
    private readonly IStudentActivityLogRepository _activityLogRepository;
    private readonly IUnitOfWork                   _unitOfWork;
    private readonly ILogger<UploadStudentImageCommandHandler> _logger;

    public UploadStudentImageCommandHandler(
        IStudentRepository            studentRepository,
        IImageService                 imageService,
        IStudentActivityLogRepository activityLogRepository,
        IUnitOfWork                   unitOfWork,
        ILogger<UploadStudentImageCommandHandler> logger)
    {
        _studentRepository      = studentRepository;
        _imageService           = imageService;
        _activityLogRepository  = activityLogRepository;
        _unitOfWork             = unitOfWork;
        _logger                 = logger;
    }

    public async Task<Result<string>> Handle(UploadStudentImageCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UploadStudentImage: starting for student {StudentId}, file {FileName} ({Length} bytes)",
            request.StudentId, request.FileName, request.Length);

        var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
        if (student is null)
        {
            _logger.LogInformation("UploadStudentImage: student {StudentId} not found", request.StudentId);
            return Result.Failure<string>(StudentErrors.NotFound);
        }

        _logger.LogInformation("UploadStudentImage: validating file for student {StudentId}", student.Id);
        var validation = _imageService.ValidateFile(
            request.Length, request.ContentType, StudentAttachmentSettings.MaxBytes, StudentAttachmentSettings.AllowedContentTypes);

        var imageError = validation switch
        {
            FileValidationResult.Empty       => StudentErrors.ImageEmpty,
            FileValidationResult.TooLarge    => StudentErrors.ImageTooLarge,
            FileValidationResult.InvalidType => StudentErrors.ImageInvalidType,
            _ => null
        };

        if (imageError is not null)
        {
            _logger.LogInformation("UploadStudentImage: validation failed — {ValidationResult}", validation);
            return Result.Failure<string>(imageError);
        }

        string imageUrl;
        try
        {
            _logger.LogInformation("UploadStudentImage: uploading to image store for student {StudentId}", student.Id);
            imageUrl = await _imageService.UploadAsync(
                request.ImageStream,
                request.FileName,
                request.ContentType,
                StudentAttachmentSettings.Folder,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UploadStudentImage: upload to image store failed for student {StudentId}", student.Id);
            return Result.Failure<string>(StudentErrors.ImageUploadFailed);
        }

        student.ProfileImageUrl = imageUrl;

        _studentRepository.Update(student);

        _logger.LogInformation("UploadStudentImage: writing activity log entry");
        _activityLogRepository.Add(StudentActivityLogMappings.NewLog(
            student.TenantId, student.BranchId, student.Id,
            StudentActivityType.ImageUploaded, $"Profile image was uploaded for {student.FullName}.",
            request.UploadedByEmail, request.UploadedByName));

        _logger.LogInformation("UploadStudentImage: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UploadStudentImage: succeeded — student {StudentId} image updated", student.Id);
        return Result.Success(imageUrl);
    }
}
