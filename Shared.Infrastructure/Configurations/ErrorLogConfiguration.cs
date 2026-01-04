using Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Shared.Infrastructure.Configurations
{
    internal sealed class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
    {
        public void Configure(EntityTypeBuilder<ErrorLog> builder)
        {
            builder.ToTable("ErrorLogs");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(e => e.StackTrace)
                .HasMaxLength(8000);

            builder.Property(e => e.InnerException)
                .HasMaxLength(4000);

            builder.Property(e => e.ExceptionType)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(e => e.RequestPath)
                .HasMaxLength(500);

            builder.Property(e => e.RequestMethod)
                .HasMaxLength(10);

            builder.Property(e => e.QueryString)
                .HasMaxLength(2000);

            builder.Property(e => e.RequestBody)
                .HasMaxLength(8000);

            builder.Property(e => e.UserAgent)
                .HasMaxLength(500);

            builder.Property(e => e.IpAddress)
                .HasMaxLength(50);

            builder.Property(e => e.UserId)
                .HasMaxLength(100);

            builder.Property(e => e.TenantId)
                .HasMaxLength(100);

            builder.Property(e => e.TraceId)
                .HasMaxLength(100);

            builder.Property(e => e.Severity)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(e => e.Timestamp)
                .IsRequired();

            builder.Property(e => e.IsResolved)
                .IsRequired();

            builder.Property(e => e.ResolutionNotes)
                .HasMaxLength(2000);

            builder.Property(e => e.ResolvedBy)
                .HasMaxLength(100);

            builder.Property(e => e.AdditionalData)
                .HasMaxLength(8000);

            // Indexes
            builder.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_ErrorLogs_Timestamp");

            builder.HasIndex(e => e.Severity)
                .HasDatabaseName("IX_ErrorLogs_Severity");

            builder.HasIndex(e => e.IsResolved)
                .HasDatabaseName("IX_ErrorLogs_IsResolved");

            builder.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_ErrorLogs_TenantId");

            builder.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_ErrorLogs_UserId");

            builder.HasIndex(e => e.TraceId)
                .HasDatabaseName("IX_ErrorLogs_TraceId");
        }
    }
}
