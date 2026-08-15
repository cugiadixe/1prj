using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CemeteryConfiguration : IEntityTypeConfiguration<Cemetery>
{
    public void Configure(EntityTypeBuilder<Cemetery> builder)
    {
        builder.ToTable("Cemeteries", "dbo");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.CemeteryCode).HasColumnName("cemetery_code").HasMaxLength(50);
        builder.Property(c => c.CompanyId).HasColumnName("company_id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(c => c.Address).HasColumnName("address").HasMaxLength(500);
        builder.Property(c => c.IsActive).HasColumnName("is_active");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(c => c.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasOne(c => c.Company)
            .WithMany()
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
