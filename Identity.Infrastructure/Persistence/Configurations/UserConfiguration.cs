using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for User entity
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.EmailConfirmed)
            .IsRequired();

        builder.Property(u => u.LastLoginDate);

        builder.Property(u => u.FailedLoginAttempts)
            .IsRequired();

        builder.Property(u => u.LockoutEnd);

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(500);

        builder.Property(u => u.RefreshTokenExpiryTime);

        builder.Property(u => u.CreatedOn)
            .IsRequired();

        builder.Property(u => u.ModifiedOn);

        builder.Property(t => t.CreatedByEmail)
            .IsRequired();

        builder.Property(t => t.CreatedByName)
            .IsRequired();

        builder.Property(u => u.TenantId)
            .IsRequired();

        // Configure roles as JSON (or separate table if preferred)
        builder.Property(u => u.Roles)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasMaxLength(500);

        builder.Ignore(u => u.FullName);
    }
}
