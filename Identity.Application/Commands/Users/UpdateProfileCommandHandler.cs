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
            // Get updated user
            var user = await _userRepository.GetByIdAsync(request.model.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<UserProfileDto>(UserErrors.UserNotFound);
            }

            user.FirstName = request.model.FirstName;
            user.LastName = request.model.LastName;
            user.ModifiedOn = DateTimeOffset.UtcNow;
            user.ModifiedByEmail = "admin@TKDHub.com";
            user.ModifiedByName = "Admin";

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(user.ToProfileDto());
        }
    }
}
