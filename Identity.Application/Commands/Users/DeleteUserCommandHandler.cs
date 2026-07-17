using Identity.Domain.Constants;

using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Users
{
    public sealed record DeleteUserCommand(Guid UserId, Guid RequestedByUserId) : ICommand<string>;

    internal sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, string>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, ILogger<DeleteUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("DeleteUser: starting for user {UserId}, requested by {RequestedByUserId}", request.UserId, request.RequestedByUserId);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                _logger.LogInformation("DeleteUser: user {UserId} not found", request.UserId);
                return Result.Failure<string>(UserErrors.UserNotFound);
            }

            // Users can only delete their own account unless they are an Admin
            if (request.RequestedByUserId != request.UserId)
            {
                var requestingUser = await _userRepository.GetByIdAsync(request.RequestedByUserId, cancellationToken);
                var isAdmin = requestingUser?.UserRoles.Any(r => r.RoleId == UserRoleEnum.SuberAdmin) ?? false;

                if (!isAdmin)
                {
                    _logger.LogInformation("DeleteUser: rejected — requester {RequestedByUserId} is not an admin and not the target user", request.RequestedByUserId);
                    return Result.Failure<string>(UserErrors.Forbidden);
                }
            }

            // Soft delete
            user.StatusId = (short)EntityStatusEnum.Deleted;

            _logger.LogInformation("DeleteUser: soft-deleting user {UserId}", user.Id);
            _userRepository.Update(user);

            _logger.LogInformation("DeleteUser: saving changes");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("DeleteUser: succeeded — user {UserId} deleted", user.Id);
            return Result.Success(UserMessages.UserDeletedSuccessfully);
        }
    }
}
