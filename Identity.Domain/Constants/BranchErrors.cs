using Shared.Domain.Primitives;

namespace Identity.Domain.Constants;

public static class BranchErrors
{
    public static readonly Error NotFound = new(
        "Branch.NotFound",
        "Branch not found");

    public static readonly Error NameRequired = new(
        "Branch.NameRequired",
        "Branch name is required");

    public static readonly Error EmailRequired = new(
        "Branch.EmailRequired",
        "Branch email is required");

    public static readonly Error NameAlreadyExists = new(
        "Branch.NameAlreadyExists",
        "A branch with this name already exists");

    public static readonly Error CannotDeleteSelf = new(
        "Branch.CannotDeleteSelf",
        "Cannot delete the branch you are currently operating in");
}
