using Identity.Application.Contracts;
using Identity.Application.Models.Auth;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Authentications
{
    public sealed record ChangePasswordCommand(ChangePasswordModel model) : ICommand;

    internal sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChangePasswordCommandHandler> _logger;

        public ChangePasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork, ILogger<ChangePasswordCommandHandler> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.model.UserId, cancellationToken);

            if (user is null)
                return Result.Failure(UserErrors.UserNotFound);

            // Verify current password
            if (!_passwordHasher.VerifyPassword(request.model.OldPassword, user.PasswordHash))
            {
                _logger.LogWarning("Password change failed — incorrect current password for user {UserId}", request.model.UserId);
                return Result.Failure(UserErrors.InvalidCredentials);
            }

            // Hash new password
            var newPasswordHash = _passwordHasher.HashPassword(request.model.NewPassword);

            // Change password
            var result = ChangePassword(user, newPasswordHash);

            if (result.IsFailure)
                return result;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password changed successfully for user {UserId}", user.Id);

            return Result.Success();
        }

        private static Result ChangePassword(User user, string newPasswordHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                return Result.Failure(UserErrors.PasswordRequired);

            user.PasswordHash = newPasswordHash;

            return Result.Success();
        }
    }
}