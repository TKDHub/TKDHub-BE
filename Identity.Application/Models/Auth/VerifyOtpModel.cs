using Identity.Domain.Enums;

namespace Identity.Application.Models.Auth
{
    public sealed record VerifyOtpModel
    {
        public string Identifier { get; init; } = string.Empty;
        public IdentifierType Type { get; init; }
        public string Otp { get; init; } = string.Empty;
    }
}
