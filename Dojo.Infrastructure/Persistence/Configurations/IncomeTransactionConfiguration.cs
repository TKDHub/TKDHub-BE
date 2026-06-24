using Dojo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dojo.Infrastructure.Persistence.Configurations;

internal sealed class IncomeTransactionConfiguration : IEntityTypeConfiguration<IncomeTransaction>
{
    public void Configure(EntityTypeBuilder<IncomeTransaction> builder)
    {
        builder.ToTable("IncomeTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.BranchId).IsRequired();
        builder.Property(t => t.IncomeInvoiceId).IsRequired();

        builder.Property(t => t.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(t => t.Method)
            .IsRequired()
            .HasConversion<short>();

        builder.HasIndex(t => t.IncomeInvoiceId)
            .HasDatabaseName("IX_IncomeTransactions_IncomeInvoiceId");

        // ── Audit ────────────────────────────────────────────────
        builder.Property(t => t.StatusId).IsRequired();
        builder.Property(t => t.CreatedOn).IsRequired();
        builder.Property(t => t.CreatedByEmail).IsRequired();
        builder.Property(t => t.CreatedByName).IsRequired();
    }
}
