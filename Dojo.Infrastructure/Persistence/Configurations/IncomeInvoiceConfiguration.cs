using Dojo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dojo.Infrastructure.Persistence.Configurations;

internal sealed class IncomeInvoiceConfiguration : IEntityTypeConfiguration<IncomeInvoice>
{
    public void Configure(EntityTypeBuilder<IncomeInvoice> builder)
    {
        builder.ToTable("IncomeInvoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.BranchId).IsRequired();
        builder.Property(i => i.StudentId).IsRequired();

        builder.Property(i => i.Type)
            .IsRequired()
            .HasConversion<short>();

        builder.Property(i => i.OriginalPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.DiscountType)
            .HasConversion<short>();   // nullable

        builder.Property(i => i.DiscountValue)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<short>();

        // ── Void audit (nullable — only set when Status == Voided) ──
        builder.Property(i => i.VoidedByEmail).HasMaxLength(150);
        builder.Property(i => i.VoidedByName).HasMaxLength(150);
        builder.Property(i => i.VoidReason).HasMaxLength(500);

        builder.HasOne(i => i.Student)
            .WithMany()
            .HasForeignKey(i => i.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Transactions)
            .WithOne(t => t.IncomeInvoice)
            .HasForeignKey(t => t.IncomeInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.StudentId)
            .HasDatabaseName("IX_IncomeInvoices_StudentId");

        // ── Derived — never persisted ────────────────────────────
        builder.Ignore(i => i.DiscountAmount);
        builder.Ignore(i => i.PriceAfterDiscount);
        builder.Ignore(i => i.AmountPaid);
        builder.Ignore(i => i.RemainingAmount);
        builder.Ignore(i => i.PaymentStatus);

        // ── Audit ────────────────────────────────────────────────
        builder.Property(i => i.StatusId).IsRequired();
        builder.Property(i => i.CreatedOn).IsRequired();
        builder.Property(i => i.CreatedByEmail).IsRequired();
        builder.Property(i => i.CreatedByName).IsRequired();
    }
}
