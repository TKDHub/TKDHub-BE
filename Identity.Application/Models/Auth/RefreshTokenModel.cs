namespace Identity.Application.Models.Auth
{
    public sealed record RefreshTokenModel
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
    }
}
