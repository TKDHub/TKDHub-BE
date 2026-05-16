using Shared.Application.Models;

namespace Shared.Application.Contracts;

/// <summary>
/// Fetches branch data from the Identity service.
/// </summary>
public interface IBranchService
{
    /// <summary>
    /// Returns the full <see cref="BranchInfo"/> for the given branch,
    /// or <c>null</c> if not found.
    /// </summary>
    Task<BranchInfo?> GetBranchAsync(Guid branchId, CancellationToken cancellationToken = default);
}
