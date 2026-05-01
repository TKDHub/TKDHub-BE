using Identity.Application.Contracts;
using Identity.Application.Models.Auth;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Authentications
{
    public sealed record ResetPasswordCommand(ResetPasswordModel model) : ICommand<string>;

    internal sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, string>
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

        public async Task<Result<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.model.NewPassword))
                return Result.Failure<string>(UserErrors.PasswordRequired);

            if (request.model.NewPassword != request.model.ConfirmPassword)
                return Result.Failure<string>(UserErrors.PasswordMismatch);

            var identifier = request.model.Identifier.Trim();

            var user = await _userRepository.GetByEmailAsync(identifier, cancellationToken)
                    ?? await _userRepository.GetByPhoneAsync(identifier, cancellationToken);

            if (user is null)
                return Result.Failure<string>(UserErrors.UserNotFound);

            // PasswordResetToken must be a UUID (swapped in after OTP verification) and not expired
            if (user.PasswordResetToken is null
                || !Guid.TryParse(user.PasswordResetToken, out _)
                || user.PasswordResetTokenExpiryTime is null
                || user.PasswordResetTokenExpiryTime < DateTime.UtcNow)
            {
                return Result.Failure<string>(OtpErrors.NotVerified);
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.model.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiryTime = null;
            user.ModifiedOn = DateTimeOffset.UtcNow;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password reset successfully for {Identifier}", identifier);

            return Result.Success(UserMessages.PasswordResetSuccessfully);
        }
    }
}
