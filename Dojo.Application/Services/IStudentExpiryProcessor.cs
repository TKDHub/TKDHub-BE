namespace Dojo.Application.Services;

public interface IStudentExpiryProcessor
{
    /// <summary>Deactivates every Active student whose EndDate has passed and notifies the
    /// student plus their branch's Admins/SuperAdmins. Returns how many were processed.</summary>
    Task<int> ProcessExpiredStudentsAsync(CancellationToken cancellationToken = default);
}
