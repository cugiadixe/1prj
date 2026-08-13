using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class GraveEmergencyContactConfiguration : IEntityTypeConfiguration<GraveEmergencyContact>
{
    public void Configure(EntityTypeBuilder<GraveEmergencyContact> builder)
    {
        builder.ToTable("Grave_Emergency_Contacts", "dbo");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.GraveId).HasColumnName("grave_id");
        builder.Property(c => c.Priority).HasColumnName("priority");
        builder.Property(c => c.ContactCustomerId).HasColumnName("contact_customer_id");
        builder.Property(c => c.ContactName).HasColumnName("contact_name").HasMaxLength(200);
        builder.Property(c => c.ContactPhone).HasColumnName("contact_phone").HasMaxLength(20);
        builder.Property(c => c.RelationshipNote).HasColumnName("relationship_note").HasMaxLength(100);
        builder.Property(c => c.IsActive).HasColumnName("is_active");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(c => c.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasOne(c => c.Contact)
            .WithMany()
            .HasForeignKey(c => c.ContactCustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.GraveId, c.Priority });
    }
}
