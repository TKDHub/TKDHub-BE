namespace Identity.Application.Contracts
{
    /// <summary>
    /// Password hashing service interface
    /// </summary>
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }
}
