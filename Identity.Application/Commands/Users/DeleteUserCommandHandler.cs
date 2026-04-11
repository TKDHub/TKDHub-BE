using Identity.Domain.Constants;
using Identity.Domain.Repositories;
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

        public DeleteUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<string>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
                return Result.Failure<string>(UserErrors.UserNotFound);

            // Users can only delete their own account unless they are an Admin
            if (request.RequestedByUserId != request.UserId)
            {
                var requestingUser = await _userRepository.GetByIdAsync(request.RequestedByUserId, cancellationToken);
                var isAdmin = requestingUser?.Roles.Contains(UserRoles.Admin, StringComparer.OrdinalIgnoreCase) ?? false;

                if (!isAdmin)
                    return Result.Failure<string>(UserErrors.Forbidden);
            }

            // Soft delete
            user.StatusId = (short)EntityStatusEnum.Deleted;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(UserMessages.UserDeletedSuccessfully);
        }
    }
}
