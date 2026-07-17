using Identity.Application.Models.Auth;
using Identity.Domain.Constants;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Application.Messaging;
using Shared.Domain.Constants;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;


namespace Identity.Application.Commands.Authentications
{
    public sealed record ForgotPasswordCommand(ForgotPasswordModel model) : ICommand<string>;

    internal sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, string>
    {
        private readonly IUserRepository _userRepository;
        private readonly IOtpService _otpService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;

        public ForgotPasswordCommandHandler(
            IUserRepository userRepository,
            IOtpService otpService,
            IUnitOfWork unitOfWork,
            ILogger<ForgotPasswordCommandHandler> logger)
        {
            _userRepository = userRepository;
            _otpService = otpService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var identifier = request.model.Identifier.Trim();
            _logger.LogInformation("ForgotPassword: starting for {Type} identifier", request.model.Type);

            var user = request.model.Type == IdentifierType.Email
                ? await _userRepository.GetByEmailAsync(identifier, cancellationToken)
                : await _userRepository.GetByPhoneAsync(identifier, cancellationToken);

            if (user is null)
            {
                _logger.LogWarning("Forgot password requested for unknown identifier: {Identifier}", identifier);
                return Result.Failure<string>(UserErrors.UserNotFound);
            }

            _logger.LogInformation("ForgotPassword: generating OTP for user {UserId}", user.Id);
            var otp = _otpService.GenerateOtp();
            user.PasswordResetToken = otp;
            user.PasswordResetTokenExpiryTime = DateTime.UtcNow.AddMinutes(OtpPolicy.ExpiryMinutes);

            _userRepository.Update(user);

            _logger.LogInformation("ForgotPassword: saving changes");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("ForgotPassword: sending OTP");
            var result = await _otpService.SendOtpAsync(identifier, request.model.Type, otp, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning("ForgotPassword: OTP send failed for user {UserId} — {ErrorCode}", user.Id, result.Error.Code);
                return Result.Failure<string>(result.Error);
            }

            _logger.LogInformation("OTP generated for {Type} {Identifier}", request.model.Type, identifier);

            return Result.Success(OtpMessages.OtpSent);
        }
    }
}