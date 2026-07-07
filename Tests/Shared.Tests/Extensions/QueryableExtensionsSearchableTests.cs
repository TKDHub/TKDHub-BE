using Identity.Domain.Entities;
using Shared.Domain.Pagination;
using Shared.Domain.Primitives;
using Shared.Infrastructure.Extensions;

namespace Shared.Tests.Extensions;

/// <summary>
/// Proves the [Searchable] allow-list closes the dynamic-filter/sort column-probing hole:
/// a property not decorated with [Searchable] must be completely inert as a filter or sort
/// target — not partially honored, not throwing, just silently ignored (fail-closed).
/// </summary>
public class QueryableExtensionsSearchableTests
{
    private sealed class Widget
    {
        [Searchable] public string Name { get; set; } = string.Empty;
        [Searchable] public int Quantity { get; set; }
        // Deliberately NOT [Searchable] — stands in for PasswordHash/RefreshToken/etc.
        public string Secret { get; set; } = string.Empty;
        [Searchable] public Nested? Detail { get; set; }
    }

    private sealed class Nested
    {
        [Searchable] public string City { get; set; } = string.Empty;
        // Not searchable even though the parent (Detail) is — every hop must opt in.
        public string PrivateNote { get; set; } = string.Empty;
    }

    private static IQueryable<Widget> Widgets() => new List<Widget>
    {
        new() { Name = "Alpha", Quantity = 1, Secret = "s3cr3t-alpha", Detail = new Nested { City = "Amman", PrivateNote = "no-one-should-see-this" } },
        new() { Name = "Beta",  Quantity = 2, Secret = "s3cr3t-beta",  Detail = new Nested { City = "Zarqa",  PrivateNote = "confidential" } },
    }.AsQueryable();

    private static FilterCriteria Filter(string column, FilterOperator op, string value) =>
        new() { Column = column, Operator = op, Value = value };

    [Fact]
    public void ApplyFilter_OnSearchableProperty_FiltersAsExpected()
    {
        var result = Widgets().ApplyFilter(Filter("Name", FilterOperator.Equals, "Alpha")).ToList();

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public void ApplyFilter_OnNonSearchableProperty_IsSilentlyIgnored()
    {
        // If this leaked through, only the row whose Secret starts with "s3cr3t-a" would return.
        var result = Widgets().ApplyFilter(Filter("Secret", FilterOperator.StartsWith, "s3cr3t-a")).ToList();

        // Fail-closed: the filter is a no-op, so BOTH rows still come back.
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ApplyFilter_OnUnknownColumn_IsSilentlyIgnored()
    {
        var result = Widgets().ApplyFilter(Filter("DoesNotExist", FilterOperator.Equals, "x")).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ApplyFilter_OnNestedSearchableProperty_FiltersAsExpected()
    {
        var result = Widgets().ApplyFilter(Filter("Detail.City", FilterOperator.Equals, "Amman")).ToList();

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Name);
    }

    [Fact]
    public void ApplyFilter_OnNestedNonSearchableProperty_IsSilentlyIgnored()
    {
        var result = Widgets().ApplyFilter(Filter("Detail.PrivateNote", FilterOperator.StartsWith, "conf")).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ApplySort_OnSearchableProperty_SortsAsExpected()
    {
        var result = Widgets().ApplySort("Quantity", descending: true).ToList();

        Assert.Equal("Beta", result[0].Name);
        Assert.Equal("Alpha", result[1].Name);
    }

    [Fact]
    public void ApplySort_OnNonSearchableProperty_LeavesQueryUnsorted()
    {
        var original = Widgets().ToList();

        var result = Widgets().ApplySort("Secret", descending: true).ToList();

        // Fail-closed: no ordering applied, so original enumeration order is preserved.
        Assert.Equal(original.Select(w => w.Name), result.Select(w => w.Name));
    }

    [Fact]
    public void ApplySort_OnUnknownColumn_LeavesQueryUnsorted()
    {
        var original = Widgets().ToList();

        var result = Widgets().ApplySort("NoSuchColumn", descending: false).ToList();

        Assert.Equal(original.Select(w => w.Name), result.Select(w => w.Name));
    }

    // ── Direct regression test against the real, previously-vulnerable entity ──────────

    private static readonly Guid UserAId = Guid.NewGuid();
    private static readonly Guid UserBId = Guid.NewGuid();

    // Fixed IDs (not freshly generated per call) so "before" and "after" snapshots in the
    // same test are directly comparable — this is the same list's data, not two different sets.
    private static List<User> UserList() => new()
    {
        new()
        {
            Id = UserAId, Username = "jdoe", Email = "jdoe@acme.test",
            PasswordHash = "hash-abc123", RefreshToken = "refresh-abc123", PasswordResetToken = "reset-abc123",
            CreatedOn = DateTimeOffset.UtcNow, CreatedByEmail = "a", CreatedByName = "A"
        },
        new()
        {
            Id = UserBId, Username = "asmith", Email = "asmith@acme.test",
            PasswordHash = "hash-xyz789", RefreshToken = "refresh-xyz789", PasswordResetToken = "reset-xyz789",
            CreatedOn = DateTimeOffset.UtcNow, CreatedByEmail = "a", CreatedByName = "A"
        },
    };

    private static IQueryable<User> Users() => UserList().AsQueryable();

    [Fact]
    public void ApplyFilter_OnUser_Username_Works()
    {
        var result = Users().ApplyFilter(Filter("Username", FilterOperator.Equals, "jdoe")).ToList();

        Assert.Single(result);
        Assert.Equal("jdoe", result[0].Username);
    }

    [Theory]
    [InlineData("PasswordHash", "hash-abc")]
    [InlineData("RefreshToken", "refresh-abc")]
    [InlineData("PasswordResetToken", "reset-abc")]
    public void ApplyFilter_OnUser_SecretFields_CannotBeProbed(string column, string prefixGuess)
    {
        // An attacker trying to extract a secret character-by-character via StartsWith
        // must get back the FULL unfiltered set every time — never a narrowed-down result
        // that would reveal whether the guessed prefix was correct.
        var result = Users().ApplyFilter(Filter(column, FilterOperator.StartsWith, prefixGuess)).ToList();

        Assert.Equal(2, result.Count);
    }

    [Theory]
    [InlineData("PasswordHash")]
    [InlineData("RefreshToken")]
    [InlineData("PasswordResetToken")]
    public void ApplySort_OnUser_SecretFields_CannotLeakRelativeOrdering(string column)
    {
        var unsorted = Users().ToList();

        var result = Users().ApplySort(column, descending: false).ToList();

        Assert.Equal(unsorted.Select(u => u.Id), result.Select(u => u.Id));
    }
}
