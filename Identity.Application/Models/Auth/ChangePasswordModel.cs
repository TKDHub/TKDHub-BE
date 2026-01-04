namespace Identity.Application.Models.Auth
{
    public sealed record ChangePasswordModel
    {
        public Guid UserId { set; get; }
        public string CurrentPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
        public string ConfirmNewPassword { get; init; } = string.Empty;
    }
}
