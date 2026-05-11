namespace Dojo.Application.Dtos.Students;

public sealed class StudentDto
{
    public Guid   Id             { get; init; }
    public Guid   TenantId       { get; init; }
    public Guid   BranchId       { get; init; }

    public string FirstName      { get; init; } = string.Empty;
    public string LastName       { get; init; } = string.Empty;
    public string FullName       { get; init; } = string.Empty;
    public string Email          { get; init; } = string.Empty;
    public string? PhoneNumber   { get; init; }

    public DateOnly DateOfBirth    { get; init; }
    public string   Gender         { get; init; } = string.Empty;
    public string   BeltLevel      { get; init; } = string.Empty;
    public DateOnly EnrollmentDate { get; init; }
    public bool     Enabled        { get; init; }
}
