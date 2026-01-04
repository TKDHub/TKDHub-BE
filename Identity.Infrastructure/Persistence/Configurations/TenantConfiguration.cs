using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("Tenants");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Subdomain)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(t => t.Subdomain)
                .IsUnique()
                .HasDatabaseName("IX_Tenants_Subdomain");

            builder.Property(t => t.ContactEmail)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.StatusId)
                .IsRequired();

            builder.Property(t => t.SubscriptionPlan)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.MaxUsers)
                .IsRequired();

            builder.Property(t => t.CreatedOn)
                .IsRequired();
            
            builder.Property(t => t.CreatedByEmail)
                .IsRequired();
            
            builder.Property(t => t.CreatedByName)
                .IsRequired();
        }
    }
}