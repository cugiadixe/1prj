using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.ToTable("Service_Types");

        builder.HasKey(e => e.Id).HasName("PK_Service_Types");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(e => e.StandardPrice).HasColumnName("standard_price").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.StandardPriceCurrency).HasColumnName("standard_price_currency").HasMaxLength(3).IsRequired();
        builder.Property(e => e.CycleDurationMonths).HasColumnName("cycle_duration_months");
        builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();

        builder.HasIndex(e => e.Code).IsUnique().HasDatabaseName("UQ_Service_Types_code");
        builder.HasIndex(e => e.IsActive).HasDatabaseName("IX_Service_Types_is_active");
    }
}
