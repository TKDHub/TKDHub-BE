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

        builder.Property(s => s.BranchId)
            .IsRequired();

        builder.Property(s => s.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(s => new { s.TenantId, s.Email })
            .IsUnique()
            .HasDatabaseName("IX_Students_TenantId_Email");

        builder.Property(s => s.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(s => s.DateOfBirth)
            .IsRequired();

        builder.Property(s => s.Gender)
            .IsRequired()
            .HasConversion<short>();

        builder.Property(s => s.BeltLevel)
            .IsRequired()
            .HasConversion<short>();

        builder.Property(s => s.EnrollmentDate)
            .IsRequired();

        builder.Property(s => s.Enabled)
            .IsRequired();

        builder.Ignore(s => s.FullName);

        builder.Property(s => s.StatusId).IsRequired();
        builder.Property(s => s.CreatedOn).IsRequired();
        builder.Property(s => s.CreatedByEmail).IsRequired();
        builder.Property(s => s.CreatedByName).IsRequired();
    }
}
