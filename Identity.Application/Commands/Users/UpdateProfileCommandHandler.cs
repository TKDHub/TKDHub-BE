using Identity.Application.Dtos.Users;
using Identity.Application.Mappings.Users;
using Identity.Application.Models.User;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Users
{
    public sealed record UpdateProfileCommand(UpdateUserModel model) : ICommand<UserProfileDto>;

    internal sealed class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand, UserProfileDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateProfileCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UserProfileDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.model.Username))
                return Result.Failure<UserProfileDto>(UserErrors.UsernameRequired);

            var user = await _userRepository.GetByIdAsync(request.model.UserId, cancellationToken);
            if (user is null)
                return Result.Failure<UserProfileDto>(UserErrors.UserNotFound);

            var newUsername = request.model.Username.Trim();
            if (!string.Equals(user.Username, newUsername, StringComparison.OrdinalIgnoreCase))
            {
                var taken = await _userRepository.ExistsByUsernameAsync(newUsername, cancellationToken);
                if (taken)
                    return Result.Failure<UserProfileDto>(UserErrors.UsernameAlreadyExists);
            }

            user.Username = newUsername;
            user.PhoneNumber = request.model.PhoneNumber?.Trim();
            user.ModifiedOn = DateTimeOffset.UtcNow;
            user.ModifiedByEmail = request.model.ModifiedByEmail;
            user.ModifiedByName = request.model.ModifiedByName;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(user.ToProfileDto());
        }
    }
}
