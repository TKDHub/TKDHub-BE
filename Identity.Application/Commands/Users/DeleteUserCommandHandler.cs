using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Shared.Application.Messaging;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Users
{
    public sealed record DeleteUserCommand(Guid userId) : ICommand<string>;

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
            // Get user
            var user = await _userRepository.GetByIdAsync(request.userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<string>(UserErrors.UserNotFound);
            }

            // Soft delete (set StatusId = 3)
            user.StatusId = (short)EntityStatusEnum.Deleted;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(UserMessages.UserDeletedSuccessfully);
        }
    }
}
