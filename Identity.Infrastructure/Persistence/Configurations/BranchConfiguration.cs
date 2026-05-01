using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(b => new { b.TenantId, b.Name })
            .IsUnique()
            .HasDatabaseName("IX_Branches_TenantId_Name");

        builder.Property(b => b.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(b => b.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(b => b.Enabled)
            .IsRequired();

        builder.Property(b => b.AddressCountry)
            .HasMaxLength(100);

        builder.Property(b => b.AddressState)
            .HasMaxLength(100);

        builder.Property(b => b.AddressCity)
            .HasMaxLength(100);

        builder.Property(b => b.AddressStreet)
            .HasMaxLength(250);

        builder.Property(b => b.TimeZone);

        builder.Property(b => b.StatusId)
            .IsRequired();

        builder.Property(b => b.CreatedOn)
            .IsRequired();

        builder.Property(b => b.CreatedByEmail)
            .IsRequired();

        builder.Property(b => b.CreatedByName)
            .IsRequired();

        // Tenant → Branches (one-to-many)
        builder.HasOne(b => b.Tenant)
            .WithMany(t => t.Branches)
            .HasForeignKey(b => b.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Branch ↔ Users (many-to-many via UserBranches join table)
        builder.HasMany(b => b.Users)
            .WithMany(u => u.Branches)
            .UsingEntity("UserBranches",
                r => r.HasOne(typeof(User)).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade),
                l => l.HasOne(typeof(Branch)).WithMany().HasForeignKey("BranchId").OnDelete(DeleteBehavior.Cascade));
    }
}
