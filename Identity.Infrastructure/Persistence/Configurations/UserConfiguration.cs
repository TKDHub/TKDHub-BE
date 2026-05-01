using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(50);

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

        builder.Property(u => u.CreatedByEmail)
            .IsRequired();

        builder.Property(u => u.CreatedByName)
            .IsRequired();

        builder.Property(u => u.TenantId)
            .IsRequired();

    }
}
