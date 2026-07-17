namespace Dojo.Domain.Enums;

public enum StudentActivityType : short
{
    Created              = 1,
    Updated              = 2,
    Deleted              = 3,
    Frozen               = 4,
    Reactivated          = 5,
    ImageUploaded        = 6,
    AddedToClass         = 7,
    RemovedFromClass     = 8,
    MovedClass           = 9,
    ExpiredAutomatically = 10
}
