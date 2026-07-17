using Dojo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dojo.Infrastructure.Persistence.Configurations;

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.BranchId).IsRequired();

        // ── Identity ────────────────────────────────────────────────
        builder.Property(s => s.FirstName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.LastName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Email)
            .HasMaxLength(150);   // nullable — no IsRequired()

        builder.HasIndex(s => new { s.TenantId, s.Email })
            .IsUnique()
            .HasDatabaseName("IX_Students_TenantId_Email")
            .HasFilter("\"Email\" IS NOT NULL");   // allow multiple NULLs

        builder.Property(s => s.PhoneNumber)
            .IsRequired()
            .HasMaxLength(50);

        // ── Demographics ─────────────────────────────────────────────
        builder.Property(s => s.DateOfBirth).IsRequired();

        builder.Property(s => s.Gender)
            .IsRequired()
            .HasConversion<short>();

        // ── Membership ───────────────────────────────────────────────
        builder.Property(s => s.StartDate).IsRequired();

        builder.Property(s => s.EndDate).IsRequired();

        builder.Property(s => s.BeltLevel)
            .IsRequired()
            .HasConversion<short>();

        builder.Property(s => s.SubscriptionPlanId).IsRequired();

        builder.HasOne(s => s.SubscriptionPlan)
            .WithMany(p => p.Students)
            .HasForeignKey(s => s.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);   // archiving a plan must not cascade-delete students

        // Class ↔ Student relationship (one class has many students) is configured on the
        // Class side in ClassConfiguration; this index just speeds up roster lookups by class.
        builder.HasIndex(s => s.ClassId)
            .HasDatabaseName("IX_Students_ClassId");

        // ── Snapshot ─────────────────────────────────────────────────
        builder.Property(s => s.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(s => s.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(s => s.DurationMonths).IsRequired();

        // ── Optional ─────────────────────────────────────────────────
        builder.Property(s => s.ProfileImageUrl).HasMaxLength(500);  // Cloudflare Images URL
        builder.Property(s => s.EmergencyContact).HasMaxLength(200);

        // ── Freeze audit ─────────────────────────────────────────────
        builder.Property(s => s.FrozenByEmail).HasMaxLength(150);
        builder.Property(s => s.FrozenByName).HasMaxLength(150);

        // ── Computed ─────────────────────────────────────────────────
        builder.Ignore(s => s.FullName);

        // ── Audit ────────────────────────────────────────────────────
        builder.Property(s => s.StatusId).IsRequired();
        builder.Property(s => s.CreatedOn).IsRequired();
        builder.Property(s => s.CreatedByEmail).IsRequired();
        builder.Property(s => s.CreatedByName).IsRequired();
    }
}
