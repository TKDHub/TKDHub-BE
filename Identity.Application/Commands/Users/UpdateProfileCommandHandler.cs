using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Application.Models.User;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Users
{
    public sealed record UpdateProfileCommand(UpdateUserModel model) : ICommand<UserProfileDto>;

    internal sealed class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand, UserProfileDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateProfileCommandHandler> _logger;

        public UpdateProfileCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, ILogger<UpdateProfileCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<UserProfileDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("UpdateProfile: starting for user {UserId}", request.model.UserId);

            if (string.IsNullOrWhiteSpace(request.model.Username))
            {
                _logger.LogInformation("UpdateProfile: rejected — username missing");
                return Result.Failure<UserProfileDto>(UserErrors.UsernameRequired);
            }

            var user = await _userRepository.GetByIdAsync(request.model.UserId, cancellationToken);
            if (user is null)
            {
                _logger.LogInformation("UpdateProfile: user {UserId} not found", request.model.UserId);
                return Result.Failure<UserProfileDto>(UserErrors.UserNotFound);
            }

            var newUsername = request.model.Username.Trim();
            if (!string.Equals(user.Username, newUsername, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("UpdateProfile: username changed, checking uniqueness");
                var taken = await _userRepository.ExistsByUsernameAsync(newUsername, cancellationToken);
                if (taken)
                {
                    _logger.LogInformation("UpdateProfile: rejected — username {Username} already exists", newUsername);
                    return Result.Failure<UserProfileDto>(UserErrors.UsernameAlreadyExists);
                }
            }

            user.Username = newUsername;
            user.PhoneNumber = request.model.PhoneNumber?.Trim();
            user.ModifiedOn = DateTimeOffset.UtcNow;
            user.ModifiedByEmail = request.model.ModifiedByEmail;
            user.ModifiedByName = request.model.ModifiedByName;

            _logger.LogInformation("UpdateProfile: applying update to user {UserId}", user.Id);
            _userRepository.Update(user);

            _logger.LogInformation("UpdateProfile: saving changes");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("UpdateProfile: succeeded — user {UserId} updated", user.Id);
            return Result.Success(user.ToProfileDto());
        }
    }
}
