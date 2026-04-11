namespace Identity.Application.Models.Auth
{
    public sealed record ResetPasswordModel
    {
        public string Email { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
        public string ResetKey { get; init; } = string.Empty;
    }
}
