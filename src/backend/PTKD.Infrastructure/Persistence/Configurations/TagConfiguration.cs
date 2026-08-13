using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags", "dbo");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.TagType).HasColumnName("tag_type").HasMaxLength(20);
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(50);
        builder.Property(t => t.Color).HasColumnName("color").HasMaxLength(20);
        builder.Property(t => t.IsActive).HasColumnName("is_active");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(t => t.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasIndex(t => new { t.TagType, t.Name }).IsUnique();
    }
}

public class CustomerTagConfiguration : IEntityTypeConfiguration<CustomerTag>
{
    public void Configure(EntityTypeBuilder<CustomerTag> builder)
    {
        builder.ToTable("Customer_Tags", "dbo");
        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.Id).HasColumnName("id");
        builder.Property(ct => ct.CustomerId).HasColumnName("customer_id");
        builder.Property(ct => ct.TagId).HasColumnName("tag_id");
        builder.Property(ct => ct.CreatedAt).HasColumnName("created_at");
        builder.Property(ct => ct.CreatedByUserId).HasColumnName("created_by_user_id");

        builder.HasOne(ct => ct.Tag)
            .WithMany()
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ct => new { ct.CustomerId, ct.TagId }).IsUnique();
    }
}

public class GraveTagConfiguration : IEntityTypeConfiguration<GraveTag>
{
    public void Configure(EntityTypeBuilder<GraveTag> builder)
    {
        builder.ToTable("Grave_Tags", "dbo");
        builder.HasKey(gt => gt.Id);

        builder.Property(gt => gt.Id).HasColumnName("id");
        builder.Property(gt => gt.GraveId).HasColumnName("grave_id");
        builder.Property(gt => gt.TagId).HasColumnName("tag_id");
        builder.Property(gt => gt.CreatedAt).HasColumnName("created_at");
        builder.Property(gt => gt.CreatedByUserId).HasColumnName("created_by_user_id");

        builder.HasOne(gt => gt.Tag)
            .WithMany()
            .HasForeignKey(gt => gt.TagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(gt => new { gt.GraveId, gt.TagId }).IsUnique();
    }
}
