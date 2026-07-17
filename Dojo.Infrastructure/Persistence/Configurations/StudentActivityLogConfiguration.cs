using Dojo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dojo.Infrastructure.Persistence.Configurations;

internal sealed class StudentActivityLogConfiguration : IEntityTypeConfiguration<StudentActivityLog>
{
    public void Configure(EntityTypeBuilder<StudentActivityLog> builder)
    {
        builder.ToTable("StudentActivityLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.BranchId).IsRequired();
        builder.Property(l => l.StudentId).IsRequired();

        builder.Property(l => l.ActivityType)
            .IsRequired()
            .HasConversion<short>();

        builder.Property(l => l.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasOne(l => l.Student)
            .WithMany()
            .HasForeignKey(l => l.StudentId)
            .OnDelete(DeleteBehavior.Restrict);   // students are soft-deleted, never hard-removed

        builder.HasIndex(l => new { l.StudentId, l.CreatedOn })
            .HasDatabaseName("IX_StudentActivityLogs_StudentId_CreatedOn");

        builder.Property(l => l.StatusId).IsRequired();
        builder.Property(l => l.CreatedOn).IsRequired();
        builder.Property(l => l.CreatedByEmail).IsRequired();
        builder.Property(l => l.CreatedByName).IsRequired();
    }
}
