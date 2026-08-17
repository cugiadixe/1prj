using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CardWatermarkConfiguration : IEntityTypeConfiguration<CardWatermark>
{
    public void Configure(EntityTypeBuilder<CardWatermark> builder)
    {
        builder.ToTable("Card_Watermarks", "dbo");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id).HasColumnName("id");
        builder.Property(w => w.CompanyId).HasColumnName("company_id");
        builder.Property(w => w.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(w => w.ContentType).HasColumnName("content_type").HasMaxLength(100);
        builder.Property(w => w.ImageBytes).HasColumnName("image_bytes");
        builder.Property(w => w.IsActive).HasColumnName("is_active");
        builder.Property(w => w.CreatedAt).HasColumnName("created_at");
        builder.Property(w => w.CreatedByUserId).HasColumnName("created_by_user_id");

        builder.HasIndex(w => w.CompanyId);
    }
}
