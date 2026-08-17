using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class GraveAttachmentConfiguration : IEntityTypeConfiguration<GraveAttachment>
{
    public void Configure(EntityTypeBuilder<GraveAttachment> builder)
    {
        builder.ToTable("Grave_Attachments", "dbo");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.GraveId).HasColumnName("grave_id");
        builder.Property(a => a.Category).HasColumnName("category").HasMaxLength(30);
        builder.Property(a => a.OwnershipHistoryId).HasColumnName("ownership_history_id");
        builder.Property(a => a.FileNameOriginal).HasColumnName("file_name_original").HasMaxLength(260);
        builder.Property(a => a.StoredName).HasColumnName("stored_name").HasMaxLength(80);
        builder.Property(a => a.ContentType).HasColumnName("content_type").HasMaxLength(100);
        builder.Property(a => a.SizeBytes).HasColumnName("size_bytes");
        builder.Property(a => a.HasThumbnail).HasColumnName("has_thumbnail");
        builder.Property(a => a.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(a => a.StorageBasePath).HasColumnName("storage_base_path").HasMaxLength(1000);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(a => a.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasIndex(a => new { a.GraveId, a.Category });
    }
}
