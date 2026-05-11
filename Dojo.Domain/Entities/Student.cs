using Dojo.Domain.Enums;
using Shared.Domain.Primitives;

namespace Dojo.Domain.Entities;

public sealed class Student : AuditableEntity<Guid>
{
    public Guid BranchId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public string Email     { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public DateOnly DateOfBirth { get; set; }
    public GenderEnum Gender    { get; set; }
    public BeltLevelEnum BeltLevel { get; set; }

    public DateOnly EnrollmentDate { get; set; }
    public bool Enabled { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
