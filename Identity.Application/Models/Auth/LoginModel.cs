namespace Identity.Application.Models.Auth
{
    public sealed record LoginModel
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }
}
