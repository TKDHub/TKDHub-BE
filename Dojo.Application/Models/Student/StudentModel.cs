namespace Dojo.Application.Models.Student;

public sealed record StudentModel
{
    public Guid? StudentId { get; set; }
    public Guid  BranchId  { get; init; }

    public string  FirstName    { get; init; } = string.Empty;
    public string  LastName     { get; init; } = string.Empty;
    public string  Email        { get; init; } = string.Empty;
    public string? PhoneNumber  { get; init; }

    public DateOnly DateOfBirth    { get; init; }
    public string   Gender         { get; init; } = string.Empty;
    public string   BeltLevel      { get; init; } = string.Empty;
    public DateOnly EnrollmentDate { get; init; }
    public bool     Enabled        { get; init; }

    public string  CreatedByEmail { get; set; } = string.Empty;
    public string  CreatedByName  { get; set; } = string.Empty;
    public string? ModifiedByEmail { get; set; }
    public string? ModifiedByName  { get; set; }
}
