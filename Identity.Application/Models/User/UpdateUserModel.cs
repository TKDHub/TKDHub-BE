namespace Identity.Application.Models.User
{
    public sealed record UpdateUserModel
    {
        public Guid UserId { set; get; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
    }
}
