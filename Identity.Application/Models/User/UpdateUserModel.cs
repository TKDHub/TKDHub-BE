namespace Identity.Application.Models.User
{
    public sealed record UpdateUserModel
    {
        public Guid UserId { get; set; }
        public string Username { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public List<Guid> BranchIds { get; init; } = new();

        // Set by the controller from JWT claims
        public string ModifiedByEmail { get; set; } = string.Empty;
        public string ModifiedByName { get; set; } = string.Empty;
    }
}
