using Identity.Domain.Entities;
using Shared.Domain.Enums;

namespace Identity.Infrastructure.Tests.TestData;

internal static class EntityFactory
{
    public static Tenant NewTenant(Guid? id = null, string? subdomain = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Test Tenant",
        Subdomain = subdomain ?? $"tenant-{Guid.NewGuid():N}",
        ContactEmail = "contact@test.com",
        SubscriptionPlan = "Pro",
        MaxUsers = 50,
        StatusId = (short)EntityStatusEnum.Active,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "system@tkdhub.com",
        CreatedByName = "System"
    };

    public static Branch NewBranch(Guid tenantId, string name = "Main Branch") => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        Email = $"{Guid.NewGuid():N}@branch.test",
        Enabled = true,
        StatusId = (short)EntityStatusEnum.Active,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "system@tkdhub.com",
        CreatedByName = "System"
    };

    public static User NewUser(Guid tenantId, string? username = null, string? email = null) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Username = username ?? $"user-{Guid.NewGuid():N}",
        Email = email ?? $"{Guid.NewGuid():N}@user.test",
        PasswordHash = "hashed-password",
        EmailConfirmed = true,
        FailedLoginAttempts = 0,
        StatusId = (short)EntityStatusEnum.Active,
        CreatedOn = DateTimeOffset.UtcNow,
        CreatedByEmail = "system@tkdhub.com",
        CreatedByName = "System"
    };

    public static UserRole NewUserRole(Guid userId, UserRoleEnum role) => new()
    {
        UserId = userId,
        RoleId = role
    };
}
