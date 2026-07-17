using Shared.Domain.Enums;

namespace Identity.Application.Models.Auth
{
    public sealed record ForgotPasswordModel
    {
        public string Identifier { get; init; } = string.Empty;
        public IdentifierType Type { get; init; }
    }
}
