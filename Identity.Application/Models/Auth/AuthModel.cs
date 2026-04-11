namespace Identity.Application.Models.Auth
{
    public sealed record AuthModel
    {
        /// <summary>Username (email address used as login identifier)</summary>
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }
}
