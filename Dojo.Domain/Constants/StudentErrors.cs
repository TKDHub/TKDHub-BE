using Shared.Domain.Primitives;

namespace Dojo.Domain.Constants;

public static class StudentErrors
{
    public static readonly Error NotFound          = new("Student.NotFound",        "Student not found.");
    public static readonly Error FirstNameRequired = new("Student.FirstNameRequired","First name is required.");
    public static readonly Error LastNameRequired  = new("Student.LastNameRequired", "Last name is required.");
    public static readonly Error EmailRequired     = new("Student.EmailRequired",    "Email is required.");
    public static readonly Error EmailAlreadyExists= new("Student.EmailExists",      "A student with this email already exists.");
    public static readonly Error BranchRequired    = new("Student.BranchRequired",   "Branch is required.");
}
