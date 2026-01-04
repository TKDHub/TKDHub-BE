namespace Identity.Application.Dtos.Users
{
    /// <summary>
    /// Authentication response (login)
    /// </summary>
    public sealed record AuthDto
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; init; }
        public IEnumerable<string> Roles { get; init; } = new List<string>();
    }
}
