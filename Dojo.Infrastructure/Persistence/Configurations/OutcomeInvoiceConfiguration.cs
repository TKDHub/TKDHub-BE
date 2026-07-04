using Dojo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dojo.Infrastructure.Persistence.Configurations;

internal sealed class OutcomeInvoiceConfiguration : IEntityTypeConfiguration<OutcomeInvoice>
{
    public void Configure(EntityTypeBuilder<OutcomeInvoice> builder)
    {
        builder.ToTable("OutcomeInvoices");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.BranchId).IsRequired();

        builder.Property(o => o.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(o => o.AttachmentUrl).HasMaxLength(500);
        builder.Property(o => o.Note).HasMaxLength(1000);

        builder.HasIndex(o => o.BranchId)
            .HasDatabaseName("IX_OutcomeInvoices_BranchId");

        // ── Audit ────────────────────────────────────────────────
        builder.Property(o => o.StatusId).IsRequired();
        builder.Property(o => o.CreatedOn).IsRequired();
        builder.Property(o => o.CreatedByEmail).IsRequired();
        builder.Property(o => o.CreatedByName).IsRequired();
    }
}
