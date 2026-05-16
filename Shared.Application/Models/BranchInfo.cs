namespace Shared.Application.Models;

/// <summary>
/// Lightweight branch projection returned by <see cref="Contracts.IBranchService"/>.
/// Keeps Shared.Application free from a direct reference to Identity.Application.
/// </summary>
public sealed record BranchInfo
{
    public Guid    Id       { get; init; }
    public Guid    TenantId { get; init; }
    public string? Currency { get; init; }
    public bool    Enabled  { get; init; }
}
