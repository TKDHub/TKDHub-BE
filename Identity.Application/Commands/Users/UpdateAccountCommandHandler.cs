using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Application.Models.User;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Users
{
    public sealed record UpdateAccountCommand(Guid UserId, UpdateAccountModel model) : ICommand<UserProfileDto>;

    internal sealed class UpdateAccountCommandHandler : ICommandHandler<UpdateAccountCommand, UserProfileDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateAccountCommandHandler> _logger;

        public UpdateAccountCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateAccountCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<UserProfileDto>> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("UpdateAccount: starting for user {UserId}", request.UserId);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                _logger.LogInformation("UpdateAccount: user {UserId} not found", request.UserId);
                return Result.Failure<UserProfileDto>(UserErrors.UserNotFound);
            }

            if (!string.IsNullOrWhiteSpace(request.model.Email))
            {
                var normalizedEmail = request.model.Email.Trim().ToLowerInvariant();
                if (!normalizedEmail.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("UpdateAccount: email changed, checking uniqueness");
                    var emailTaken = await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken);
                    if (emailTaken)
                    {
                        _logger.LogInformation("UpdateAccount: rejected — email already registered");
                        return Result.Failure<UserProfileDto>(UserErrors.EmailAlreadyExists);
                    }

                    user.Email = normalizedEmail;
                }
            }

            if (request.model.Active.HasValue)
            {
                user.StatusId = request.model.Active.Value
                    ? (short)EntityStatusEnum.Active
                    : (short)EntityStatusEnum.Inactive;
            }

            if (request.model.PhoneNumber is not null)
                user.PhoneNumber = request.model.PhoneNumber.Trim();

            if (request.model.Roles.Count > 0)
            {
                user.UserRoles.Clear();
                foreach (var roleId in request.model.Roles)
                    user.UserRoles.Add(new UserRole { RoleId = roleId });
            }

            user.ModifiedOn = DateTimeOffset.UtcNow;
            user.ModifiedByEmail = request.model.ModifiedByEmail;
            user.ModifiedByName = request.model.ModifiedByName;

            _logger.LogInformation("UpdateAccount: applying update to user {UserId}", user.Id);
            _userRepository.Update(user);

            _logger.LogInformation("UpdateAccount: saving changes");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Account {UserId} updated successfully", user.Id);

            return Result.Success(user.ToProfileDto());
        }
    }
}
