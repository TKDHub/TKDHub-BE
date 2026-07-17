using Dojo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dojo.Infrastructure.Persistence.Configurations;

internal sealed class ClassConfiguration : IEntityTypeConfiguration<Class>
{
    public void Configure(EntityTypeBuilder<Class> builder)
    {
        builder.ToTable("Classes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.BranchId).IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(c => new { c.TenantId, c.BranchId, c.Name })
            .IsUnique()
            .HasDatabaseName("IX_Classes_TenantId_BranchId_Name");

        builder.Property(c => c.StartTime).IsRequired();
        builder.Property(c => c.EndTime).IsRequired();

        // Stored as a native Postgres integer[] — DayOfWeek's underlying int values.
        builder.Property(c => c.Weekdays)
            .HasConversion(
                v => v.Select(d => (int)d).ToArray(),
                v => v.Select(i => (DayOfWeek)i).ToList())
            .Metadata.SetValueComparer(new ValueComparer<List<DayOfWeek>>(
                (a, b) => (a ?? new List<DayOfWeek>()).SequenceEqual(b ?? new List<DayOfWeek>()),
                v => v.Aggregate(0, (hash, d) => HashCode.Combine(hash, d)),
                v => v.ToList()));

        builder.HasMany(c => c.Students)
            .WithOne(s => s.Class)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Restrict);   // classes are soft-deleted, never hard-removed

        builder.Property(c => c.StatusId).IsRequired();
        builder.Property(c => c.CreatedOn).IsRequired();
        builder.Property(c => c.CreatedByEmail).IsRequired();
        builder.Property(c => c.CreatedByName).IsRequired();
    }
}
