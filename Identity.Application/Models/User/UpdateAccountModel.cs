using Identity.Domain.Enums;

namespace Identity.Application.Models.User
{
    public sealed record UpdateAccountModel
    {
        public Guid UserId { get; set; }
        public string? Email { get; init; }
        public bool? Active { get; init; }
        public AccountActorType? Actor { get; init; }
        public string? PhoneNumber { get; init; }

        // Set by controller from JWT claims
        public string ModifiedByEmail { get; set; } = string.Empty;
        public string ModifiedByName { get; set; } = string.Empty;
    }
}
