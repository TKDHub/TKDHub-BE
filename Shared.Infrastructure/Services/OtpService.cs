using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Shared.Application.Contracts;
using Shared.Domain.Constants;
using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Shared.Infrastructure.Services
{
    internal sealed class OtpService : IOtpService
    {
        private readonly IEmailService     _emailService;
        private readonly IWhatsAppService  _whatsAppService;
        private readonly ILogger<OtpService> _logger;

        public OtpService(IEmailService emailService, IWhatsAppService whatsAppService, ILogger<OtpService> logger)
        {
            _emailService    = emailService;
            _whatsAppService = whatsAppService;
            _logger          = logger;
        }

        public string GenerateOtp()
        {
            var bytes = new byte[4];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var number = Math.Abs(BitConverter.ToInt32(bytes, 0)) % (int)Math.Pow(10, OtpPolicy.Length);
            return number.ToString($"D{OtpPolicy.Length}");
        }

        public Task<Result<string>> SendOtpAsync(string identifier, IdentifierType type, string otp, CancellationToken cancellationToken = default)
        {
            return type switch
            {
                IdentifierType.Email => SendEmailOtpAsync(identifier, otp, cancellationToken),
                IdentifierType.Phone => SendPhoneOtpAsync(identifier, otp, cancellationToken),

                // Return a failed Result instead of a raw Task
                _ => Task.FromResult(Result.Failure<string>(new Error("InvalidType", "Unsupported identifier type")))
            };
        }

        private Task<Result<string>> SendEmailOtpAsync(string email, string otp, CancellationToken cancellationToken)
        {
            var html = OtpEmailTemplate.Build(otp, OtpPolicy.ExpiryMinutes);
            return _emailService.SendAsync(email, email, OtpMessages.EmailSubject, html, cancellationToken);
        }

        private async Task<Result<string>> SendPhoneOtpAsync(string phone, string otp, CancellationToken cancellationToken)
        {
            var message = $"Your TKDHub verification code is {otp}. It expires in {OtpPolicy.ExpiryMinutes} minutes.";
            await _whatsAppService.SendMessageAsync(phone, message, cancellationToken);

            _logger.LogInformation("OTP sent via WhatsApp to {Phone}", phone);
            return Result.Success(OtpMessages.OtpSent);
        }
    }
}
