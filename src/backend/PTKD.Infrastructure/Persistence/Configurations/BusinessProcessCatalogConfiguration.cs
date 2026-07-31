using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class BusinessProcessCatalogConfiguration : IEntityTypeConfiguration<BusinessProcessCatalog>
{
    public void Configure(EntityTypeBuilder<BusinessProcessCatalog> builder)
    {
        builder.ToTable("Business_Process_Catalog");

        builder.HasKey(e => e.ProcessCode).HasName("PK_Business_Process_Catalog");
        builder.Property(e => e.ProcessCode).HasColumnName("process_code").HasMaxLength(100);
        builder.Property(e => e.ProcessName).HasColumnName("process_name").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(e => e.IsApprovalRequired).HasColumnName("is_approval_required").IsRequired();
        builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }
}
