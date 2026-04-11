namespace Identity.Application.Models.Auth
{
    public sealed record ChangePasswordModel
    {
        public Guid UserId { set; get; }
        public string OldPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}
