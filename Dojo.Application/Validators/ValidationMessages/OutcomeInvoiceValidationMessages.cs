namespace Dojo.Application.Validators.ValidationMessages;

public static class OutcomeInvoiceValidationMessages
{
    public const string TitleRequired  = "Title is required.";
    public const string TitleMaxLength = "Title cannot exceed 200 characters.";
    public const string AmountInvalid  = "Amount must be greater than zero.";
    public const string NoteMaxLength  = "Note cannot exceed 1000 characters.";
}
