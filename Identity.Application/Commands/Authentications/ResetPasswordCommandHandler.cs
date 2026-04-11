using Identity.Application.Contracts;
using Identity.Application.Models.Auth;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Authentications
{
    public sealed record ResetPasswordCommand(ResetPasswordModel model) : ICommand;

    internal sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ResetPasswordCommandHandler> _logger;

        public ResetPasswordCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork,
            ILogger<ResetPasswordCommandHandler> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.model.Email))
                return Result.Failure(UserErrors.EmailRequired);

            if (string.IsNullOrWhiteSpace(request.model.ResetKey))
                return Result.Failure(UserErrors.InvalidRefreshToken);

            if (string.IsNullOrWhiteSpace(request.model.NewPassword))
                return Result.Failure(UserErrors.PasswordRequired);

            var normalizedEmail = request.model.Email.Trim().ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

            if (user is null)
                return Result.Failure(UserErrors.UserNotFound);

            // Validate the reset key
            if (user.PasswordResetToken is null ||
                !user.PasswordResetToken.Equals(request.model.ResetKey, StringComparison.Ordinal))
            {
                _logger.LogWarning("Invalid password reset key for user {Email}", normalizedEmail);
                return Result.Failure(UserErrors.InvalidRefreshToken);
            }

            // Check reset key expiry
            if (user.PasswordResetTokenExpiryTime.HasValue &&
                user.PasswordResetTokenExpiryTime.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired password reset key for user {Email}", normalizedEmail);
                return Result.Failure(UserErrors.RefreshTokenExpired);
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.model.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiryTime = null;
            user.ModifiedOn = DateTimeOffset.UtcNow;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password reset successfully for user {Email}", normalizedEmail);

            return Result.Success();
        }
    }
}
