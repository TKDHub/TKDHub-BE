using Dojo.Application.Dtos.Classes;
using Dojo.Application.Mappings.Classes;
using Dojo.Application.Models.Class;
using Dojo.Domain.Constants;
using Dojo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Dojo.Application.Commands.Classes;

public sealed record UpdateClassCommand(ClassModel Model) : ICommand<ClassDto>;

internal sealed class UpdateClassCommandHandler : ICommandHandler<UpdateClassCommand, ClassDto>
{
    private readonly IClassRepository _classRepository;
    private readonly IUnitOfWork      _unitOfWork;
    private readonly ILogger<UpdateClassCommandHandler> _logger;

    public UpdateClassCommandHandler(IClassRepository classRepository, IUnitOfWork unitOfWork, ILogger<UpdateClassCommandHandler> logger)
    {
        _classRepository = classRepository;
        _unitOfWork      = unitOfWork;
        _logger          = logger;
    }

    public async Task<Result<ClassDto>> Handle(UpdateClassCommand request, CancellationToken cancellationToken)
    {
        var model = request.Model;
        _logger.LogInformation("UpdateClass: starting for class {ClassId}", model.ClassId);

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            _logger.LogInformation("UpdateClass: rejected — name missing");
            return Result.Failure<ClassDto>(ClassErrors.NameRequired);
        }

        if (model.EndTime <= model.StartTime)
        {
            _logger.LogInformation("UpdateClass: rejected — end time not after start time");
            return Result.Failure<ClassDto>(ClassErrors.InvalidTimeRange);
        }

        if (model.Weekdays.Count == 0)
        {
            _logger.LogInformation("UpdateClass: rejected — no weekdays provided");
            return Result.Failure<ClassDto>(ClassErrors.WeekdaysRequired);
        }

        var trainingClass = await _classRepository.GetByIdAsync(model.ClassId!.Value, cancellationToken);
        if (trainingClass is null)
        {
            _logger.LogInformation("UpdateClass: class {ClassId} not found", model.ClassId);
            return Result.Failure<ClassDto>(ClassErrors.NotFound);
        }

        if (!string.Equals(trainingClass.Name, model.Name.Trim(), StringComparison.Ordinal))
        {
            _logger.LogInformation("UpdateClass: name changed, checking uniqueness");
            var nameExists = await _classRepository.ExistsByNameAsync(
                model.Name, trainingClass.BranchId, trainingClass.Id, cancellationToken);

            if (nameExists)
            {
                _logger.LogInformation("UpdateClass: rejected — name {Name} already exists in branch {BranchId}", model.Name, trainingClass.BranchId);
                return Result.Failure<ClassDto>(ClassErrors.NameAlreadyExists);
            }
        }

        _logger.LogInformation("UpdateClass: applying update to class {ClassId}", trainingClass.Id);
        trainingClass.ApplyUpdate(model);
        _classRepository.Update(trainingClass);

        _logger.LogInformation("UpdateClass: saving changes");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UpdateClass: succeeded — class {ClassId} updated", trainingClass.Id);
        return Result.Success(trainingClass.ToDto());
    }
}
