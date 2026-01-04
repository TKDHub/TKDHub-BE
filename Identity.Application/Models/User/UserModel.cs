using Shared.Domain.Enums;

namespace Identity.Application.Models.User
{
    public sealed record UserModel
    {
        public Guid Id { get; init; }
        public Guid TenantId { get; set; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public EntityStatusEnum Status { get; init; }
        public bool EmailConfirmed { get; init; }
        public DateTime? LastLoginDate { get; init; }
        public DateTime CreatedOn { get; init; }
        public DateTime? ModifiedOn { get; init; }
    }
}
