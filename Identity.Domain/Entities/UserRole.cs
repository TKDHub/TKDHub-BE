using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public UserRoleEnum RoleId { get; set; }
    public User? User { get; set; }
}
