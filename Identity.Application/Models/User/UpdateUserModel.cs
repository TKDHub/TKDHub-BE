namespace Identity.Application.Models.User
{
    public sealed record UpdateUserModel
    {
        public Guid UserId { get; set; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;

        // Set by the controller from JWT claims — tracks who performed the update
        public string ModifiedByEmail { get; set; } = string.Empty;
        public string ModifiedByName { get; set; } = string.Empty;
    }
}
