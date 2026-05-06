using Identity.Domain.Enums;
using Shared.Domain.Primitives;

namespace Identity.Application.Contracts
{
    public interface IOtpService
    {
        string GenerateOtp();
        Task<Result<string>> SendOtpAsync(string identifier, IdentifierType type, string otp, CancellationToken cancellationToken = default);
    }
}
