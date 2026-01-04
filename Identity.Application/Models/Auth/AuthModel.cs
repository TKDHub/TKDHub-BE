namespace Identity.Application.Models.Auth
{
    public sealed record AuthModel
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }
}
