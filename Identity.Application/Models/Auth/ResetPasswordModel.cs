namespace Identity.Application.Models.Auth
{
    public sealed record ResetPasswordModel
    {
        public string Identifier { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}
