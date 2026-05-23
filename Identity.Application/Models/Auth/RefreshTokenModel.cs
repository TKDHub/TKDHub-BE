namespace Identity.Application.Models.Auth
{
    public sealed record RefreshTokenModel
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}
