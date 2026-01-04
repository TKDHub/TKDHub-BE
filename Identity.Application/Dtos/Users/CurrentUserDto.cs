namespace Identity.Application.Dtos.Users
{
    public sealed record CurrentUserDto(Guid UserId, string Email, IEnumerable<string> Roles);
}
