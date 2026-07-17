namespace Dojo.Application.Validators.ValidationMessages;

public static class ClassValidationMessages
{
    public const string NameRequired          = "Class name is required.";
    public const string NameMaxLength         = "Class name must not exceed 200 characters.";
    public const string EndTimeAfterStartTime = "End time must be after start time.";
    public const string WeekdaysRequired      = "At least one weekday is required.";
    public const string WeekdayInvalid        = "Weekday value is not valid.";
    public const string ClassIdRequired       = "Class ID is required.";
    public const string StudentIdsRequired    = "At least one student ID is required.";
    public const string FromClassIdRequired   = "Source class ID is required.";
    public const string ToClassIdRequired     = "Target class ID is required.";
    public const string FromToClassMustDiffer = "Source and target class must be different.";
}
