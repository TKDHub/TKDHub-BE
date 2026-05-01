using Identity.Domain.Enums;

namespace Identity.Application.Contracts
{
    public interface IOtpService
    {
        string GenerateOtp();
        Task SendOtpAsync(string identifier, IdentifierType type, string otp, CancellationToken cancellationToken = default);
    }
}
