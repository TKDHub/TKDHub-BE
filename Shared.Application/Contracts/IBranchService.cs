namespace Shared.Application.Contracts;

/// <summary>
/// Fetches branch data from the Identity service.
/// </summary>
public interface IBranchService
{
    /// <summary>
    /// Returns the ISO currency code for the given branch, or <c>null</c> if the branch
    /// is not found or has no currency configured.
    /// </summary>
    Task<string?> GetCurrencyAsync(Guid branchId, CancellationToken cancellationToken = default);
}
