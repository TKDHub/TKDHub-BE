using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Classes;

public sealed record DeleteClassCommand(Guid ClassId) : ICommand;

/// <summary>Soft-deletes a class. Blocked while it still has at least one active student.</summary>
internal sealed class DeleteClassCommandHandler : ICommandHandler<DeleteClassCommand>
{
    private readonly IClassRepository _classRepository;
    private readonly IUnitOfWork      _unitOfWork;
    private readonly ILogger<DeleteClassCommandHandler> _logger;

    public DeleteClassCommandHandler(IClassRepository classRepository, IUnitOfWork unitOfWork, ILogger<DeleteClassCommandHandler> logger)
    {
        _classRepository = classRepository;
        _unitOfWork      = unitOfWork;
        _logger          = logger;
    }

    public async Task<Result> Handle(DeleteClassCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteClass: starting for class {ClassId}", request.ClassId);

        var trainingClass = await _classRepository.GetByIdAsync(request.ClassId, cancellationToken);
        if (trainingClass is null)
        {
            _logger.LogInformation("DeleteClass: class {ClassId} not found", request.ClassId);
            return Result.Failure(ClassErrors.NotFound);
        }

        var hasActiveStudents = await _classRepository.HasActiveStudentsAsync(request.ClassId, cancellationToken);
        if (hasActiveStudents)
        {
            _logger.LogInformation("DeleteClass: rejected — class {ClassId} still has active students", request.ClassId);
            return Result.Failure(ClassErrors.HasActiveStudents);
        }

        trainingClass.StatusId   = (short)EntityStatusEnum.Deleted;
        trainingClass.ModifiedOn = DateTimeOffset.UtcNow;

        _logger.LogInformation("DeleteClass: soft-deleting class {ClassId}", trainingClass.Id);
        _classRepository.Update(trainingClass);

        _logger.LogInformation("DeleteClass: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DeleteClass: succeeded — class {ClassId} deleted", trainingClass.Id);
        return Result.Success();
    }
}
