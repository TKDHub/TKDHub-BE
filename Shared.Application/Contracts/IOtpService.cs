using Shared.Domain.Enums;
using Shared.Domain.Primitives;

namespace Shared.Application.Contracts
{
    public interface IOtpService
    {
        string GenerateOtp();
        Task<Result<string>> SendOtpAsync(string identifier, IdentifierType type, string otp, CancellationToken cancellationToken = default);
    }
}
