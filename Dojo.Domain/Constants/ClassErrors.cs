using Shared.Domain.Primitives;

namespace Dojo.Domain.Constants;

public static class ClassErrors
{
    public static readonly Error NotFound             = new("Class.NotFound",             "Class not found.");
    public static readonly Error NameRequired         = new("Class.NameRequired",         "Class name is required.");
    public static readonly Error NameAlreadyExists    = new("Class.NameExists",           "A class with this name already exists in the branch.");
    public static readonly Error InvalidTimeRange     = new("Class.InvalidTimeRange",     "End time must be after start time.");
    public static readonly Error WeekdaysRequired     = new("Class.WeekdaysRequired",     "At least one weekday is required.");
    public static readonly Error BranchRequired       = new("Class.BranchRequired",       "Branch is required.");
    public static readonly Error BranchNotFound       = new("Class.BranchNotFound",       "Branch not found.");
    public static readonly Error TenantBranchMismatch = new("Class.TenantBranchMismatch", "Branch does not belong to the specified tenant.");
    public static readonly Error HasActiveStudents    = new("Class.HasActiveStudents",    "Cannot delete a class that still has active students. Remove or move them first.");
    public static readonly Error StudentNotFound      = new("Class.StudentNotFound",      "One or more students were not found.");
    public static readonly Error NoStudentsProvided   = new("Class.NoStudentsProvided",   "At least one student ID is required.");
    public static readonly Error TargetClassNotFound  = new("Class.TargetClassNotFound",  "Target class not found.");
    public static readonly Error SameClass            = new("Class.SameClass",            "Source and target class must be different.");
}
