using Shared.Domain.Enums;

namespace Identity.Application.Dtos.Users
{
    public sealed record UserProfileDto
    {
        public Guid Id { get; init; }
        public Guid TenantId { get; set; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public EntityStatusEnum Status { get; init; }
        public bool EmailConfirmed { get; init; }
        public DateTimeOffset? LastLoginDate { get; init; }
        public DateTimeOffset CreatedOn { get; init; }
        public DateTimeOffset? ModifiedOn { get; init; }
    }
}
