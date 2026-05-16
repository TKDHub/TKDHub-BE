using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Students;

public sealed record UploadStudentImageCommand(
    Guid   StudentId,
    Stream ImageStream,
    string FileName,
    string ContentType,
    long   Length) : ICommand<string>;

internal sealed class UploadStudentImageCommandHandler : ICommandHandler<UploadStudentImageCommand, string>
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxImageBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IStudentRepository _studentRepository;
    private readonly IImageService      _imageService;
    private readonly IUnitOfWork        _unitOfWork;

    public UploadStudentImageCommandHandler(
        IStudentRepository studentRepository,
        IImageService      imageService,
        IUnitOfWork        unitOfWork)
    {
        _studentRepository = studentRepository;
        _imageService      = imageService;
        _unitOfWork        = unitOfWork;
    }

    public async Task<Result<string>> Handle(UploadStudentImageCommand request, CancellationToken cancellationToken)
    {
        var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
        if (student is null)
            return Result.Failure<string>(StudentErrors.NotFound);

        var imageError = ValidateImage(request.Length, request.ContentType);
        if (imageError is not null)
            return Result.Failure<string>(imageError);

        string imageUrl;
        try
        {
            imageUrl = await _imageService.UploadAsync(
                request.ImageStream,
                request.FileName,
                request.ContentType,
                cancellationToken);
        }
        catch
        {
            return Result.Failure<string>(StudentErrors.ImageUploadFailed);
        }

        student.ProfileImageUrl = imageUrl;
        student.ModifiedOn      = DateTimeOffset.UtcNow;

        _studentRepository.Update(student);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(imageUrl);
    }

    private static Error? ValidateImage(long length, string contentType)
    {
        if (length == 0)                                                        return StudentErrors.ImageEmpty;
        if (length > MaxImageBytes)                                             return StudentErrors.ImageTooLarge;
        if (!AllowedImageTypes.Contains(contentType.ToLowerInvariant()))        return StudentErrors.ImageInvalidType;
        return null;
    }
}
