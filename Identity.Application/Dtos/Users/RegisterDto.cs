namespace Identity.Application.Dtos.Users
{
    public sealed record RegisterDto
    {
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}
