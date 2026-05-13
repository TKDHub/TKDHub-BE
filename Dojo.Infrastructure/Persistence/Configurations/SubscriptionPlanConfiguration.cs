using Dojo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dojo.Infrastructure.Persistence.Configurations;

internal sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.BranchId)
            .IsRequired();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(p => new { p.TenantId, p.BranchId, p.Name })
            .IsUnique()
            .HasDatabaseName("IX_SubscriptionPlans_TenantId_BranchId_Name");

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.DurationMonths)
            .IsRequired();

        builder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.StatusId).IsRequired();
        builder.Property(p => p.CreatedOn).IsRequired();
        builder.Property(p => p.CreatedByEmail).IsRequired();
        builder.Property(p => p.CreatedByName).IsRequired();
    }
}
