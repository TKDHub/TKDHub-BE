using Identity.Domain.Entities;
using Shared.Domain.Primitives;

namespace Identity.Domain.Constants;

/// <summary>
/// User domain errors
/// </summary>
public static class UserErrors
{
    public static readonly Error NotFound = new(
        "User.NotFound",
        "User with the specified identifier was not found");

    public static readonly Error EmailRequired = new(
        "User.EmailRequired",
        "Email is required");

    public static readonly Error InvalidEmailFormat = new(
        "User.InvalidEmailFormat",
        "Email format is invalid");

    public static readonly Error EmailAlreadyExists = new(
        "User.EmailAlreadyExists",
        "User with the specified email already exists");

    public static readonly Error FirstNameRequired = new(
        "User.FirstNameRequired",
        "First name is required");

    public static readonly Error LastNameRequired = new(
        "User.LastNameRequired",
        "Last name is required");

    public static readonly Error PasswordRequired = new(
        "User.PasswordRequired",
        "Password is required");

    public static readonly Error InvalidCredentials = new(
        "User.InvalidCredentials",
        "Invalid email or password");

    public static readonly Error AccountLockedOut = new(
        "User.AccountLockedOut",
        "Account is locked due to multiple failed login attempts");

    public static readonly Error AccountNotActive = new(
        "User.AccountNotActive",
        "User account is not active");

    public static readonly Error EmailNotConfirmed = new(
        "User.EmailNotConfirmed",
        "Email address is not confirmed");

    public static readonly Error EmailAlreadyConfirmed = new(
        "User.EmailAlreadyConfirmed",
        "Email address is already confirmed");

    public static readonly Error InvalidRole = new(
        "User.InvalidRole",
        "Role name is invalid");

    public static readonly Error RoleAlreadyAssigned = new(
        "User.RoleAlreadyAssigned",
        "Role is already assigned to the user");

    public static readonly Error RoleNotFound = new(
        "User.RoleNotFound",
        "Role not found for the user");

    public static readonly Error InvalidRefreshToken = new(
        "User.InvalidRefreshToken",
        "Invalid refresh token");

    public static readonly Error RefreshTokenExpired = new(
        "User.RefreshTokenExpired",
        "Refresh token has expired");
    
    public static readonly Error UserNotFound = new(
        "User.NotFound",
        "User not found");

    public static readonly Error Forbidden = new(
        "User.Forbidden",
        "You do not have permission to perform this action");
}
