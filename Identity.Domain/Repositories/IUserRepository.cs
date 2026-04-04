namespace Identity.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<Entities.User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<List<Entities.User>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Entities.User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task<List<Entities.User>> GetByRoleAsync(string role, CancellationToken cancellationToken = default);
        void Add(Entities.User user);
        void Update(Entities.User user);
        void Remove(Entities.User user);
    }

}
