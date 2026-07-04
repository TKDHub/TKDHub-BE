using Dojo.Domain.Entities;
using Dojo.Domain.Enums;
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

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<short>()
            .HasDefaultValue(IncomeTransactionStatusEnum.Paid);

        // Plain nullable link back to the Paid transaction a Refund row offsets.
        // No FK constraint — validated in the application layer, avoids self-referencing
        // cascade-path issues on a table that is otherwise cascade-deleted from the invoice.
        builder.Property(t => t.RefundOfTransactionId);
        builder.Property(t => t.RefundedByEmail).HasMaxLength(150);
        builder.Property(t => t.RefundedByName).HasMaxLength(150);
        builder.Property(t => t.RefundReason).HasMaxLength(500);

        builder.HasIndex(t => t.IncomeInvoiceId)
            .HasDatabaseName("IX_IncomeTransactions_IncomeInvoiceId");

        builder.HasIndex(t => t.RefundOfTransactionId)
            .HasDatabaseName("IX_IncomeTransactions_RefundOfTransactionId");

        // ── Audit ────────────────────────────────────────────────
        builder.Property(t => t.StatusId).IsRequired();
        builder.Property(t => t.CreatedOn).IsRequired();
        builder.Property(t => t.CreatedByEmail).IsRequired();
        builder.Property(t => t.CreatedByName).IsRequired();
    }
}
