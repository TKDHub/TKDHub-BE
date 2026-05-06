using System.Security.Cryptography;
using Identity.Application.Contracts;
using Identity.Domain.Constants;
using Identity.Domain.Enums;
using Microsoft.Extensions.Logging;
using Shared.Domain.Primitives;

namespace Identity.Infrastructure.Services
{
    internal sealed class OtpService : IOtpService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<OtpService> _logger;

        public OtpService(IEmailService emailService, ILogger<OtpService> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public string GenerateOtp()
        {
            // TODO: switch back to random generation before production
            return OtpPolicy.StaticOtp;

            // var bytes = new byte[4];
            // using var rng = RandomNumberGenerator.Create();
            // rng.GetBytes(bytes);
            // var number = Math.Abs(BitConverter.ToInt32(bytes, 0)) % (int)Math.Pow(10, OtpPolicy.Length);
            // return number.ToString($"D{OtpPolicy.Length}");
        }

        public Task<Result<string>> SendOtpAsync(string identifier, IdentifierType type, string otp, CancellationToken cancellationToken = default)
        {
            return type switch
            {
                IdentifierType.Email => SendEmailOtpAsync(identifier, otp, cancellationToken),
                IdentifierType.Phone => SendPhoneOtpAsync(identifier, otp),

                // Return a failed Result instead of a raw Task
                _ => Task.FromResult(Result.Failure<string>(new Error("InvalidType", "Unsupported identifier type")))
            };
        }

        private Task<Result<string>> SendEmailOtpAsync(string email, string otp, CancellationToken cancellationToken)
        {
            var html = OtpEmailTemplate.Build(otp, OtpPolicy.ExpiryMinutes);
            return _emailService.SendAsync(email, email, OtpMessages.EmailSubject, html, cancellationToken);
        }

        private Task<Result<string>> SendPhoneOtpAsync(string phone, string otp)
        {
            // SMS / WhatsApp integration pending
            _logger.LogInformation("[OTP-SMS] Pending integration — OTP {Otp} for {Phone}", otp, phone);
            return Task.FromResult(Result.Success(OtpMessages.OtpSent));
        }
    }
}
