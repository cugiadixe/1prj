using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id).HasName("PK_Customers");
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.CustomerCode).HasColumnName("customer_code").HasMaxLength(50).IsRequired();
        builder.Property(c => c.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(c => c.CustomerStatus).HasColumnName("customer_status").HasMaxLength(20).IsRequired();
        builder.Property(c => c.SurvivorCustomerId).HasColumnName("survivor_customer_id");
        builder.Property(c => c.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.UpdatedByUserId).HasColumnName("updated_by_user_id");

        builder.HasIndex(c => c.CustomerCode).IsUnique().HasDatabaseName("UQ_Customers_customer_code");

        builder.HasOne(c => c.Profile)
            .WithMany()
            .HasForeignKey(c => c.ProfileId)
            .HasConstraintName("FK_Customers_profile_id");
    }
}
