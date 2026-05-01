using Identity.Application.Models.Auth;
using Identity.Domain.Constants;
using Identity.Domain.Enums;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Messaging;
using Shared.Domain.Primitives;

namespace Identity.Application.Commands.Authentications
{
    public sealed record VerifyOtpCommand(VerifyOtpModel model) : ICommand<string>;

    internal sealed class VerifyOtpCommandHandler : ICommandHandler<VerifyOtpCommand, string>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VerifyOtpCommandHandler> _logger;

        public VerifyOtpCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<VerifyOtpCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
        {
            var identifier = request.model.Identifier.Trim();

            var user = request.model.Type == IdentifierType.Email
                ? await _userRepository.GetByEmailAsync(identifier, cancellationToken)
                : await _userRepository.GetByPhoneAsync(identifier, cancellationToken);

            if (user is null
                || user.PasswordResetToken != request.model.Otp
                || user.PasswordResetTokenExpiryTime is null
                || user.PasswordResetTokenExpiryTime < DateTime.UtcNow)
            {
                return Result.Failure<string>(OtpErrors.InvalidOrExpired);
            }

            // Replace the OTP with a reset token to signal verification is complete
            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(OtpPolicy.ResetTokenExpiryMinutes);

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("OTP verified for {Identifier}", identifier);

            return Result.Success(OtpMessages.OtpVerified);
        }
    }
}
