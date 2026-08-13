using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class GraveConfiguration : IEntityTypeConfiguration<Grave>
{
    public void Configure(EntityTypeBuilder<Grave> builder)
    {
        builder.ToTable("Graves", "dbo");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id).HasColumnName("id");
        builder.Property(g => g.GraveCode).HasColumnName("grave_code").HasMaxLength(50);
        builder.Property(g => g.Zone).HasColumnName("zone").HasMaxLength(10);
        builder.Property(g => g.PlotNumber).HasColumnName("plot_number").HasMaxLength(20);
        builder.Property(g => g.RowLabel).HasColumnName("row_label").HasMaxLength(20);
        builder.Property(g => g.ColLabel).HasColumnName("col_label").HasMaxLength(20);
        builder.Property(g => g.GraveType).HasColumnName("grave_type").HasMaxLength(20);
        builder.Property(g => g.AreaM2).HasColumnName("area_m2").HasColumnType("decimal(10,2)");
        builder.Property(g => g.CotCount).HasColumnName("cot_count");
        builder.Property(g => g.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(g => g.OwnerCustomerId).HasColumnName("owner_customer_id");
        builder.Property(g => g.EmergencyContactName).HasColumnName("emergency_contact_name").HasMaxLength(200);
        builder.Property(g => g.EmergencyContactPhone).HasColumnName("emergency_contact_phone").HasMaxLength(20);
        builder.Property(g => g.EmergencyContactRelationship).HasColumnName("emergency_contact_relationship").HasMaxLength(100);
        builder.Property(g => g.Notes).HasColumnName("notes").HasMaxLength(2000);
        builder.Property(g => g.CreatedAt).HasColumnName("created_at");
        builder.Property(g => g.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(g => g.UpdatedAt).HasColumnName("updated_at");
        builder.Property(g => g.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(g => g.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasOne(g => g.Owner)
            .WithMany()
            .HasForeignKey(g => g.OwnerCustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(g => g.Occupants)
            .WithOne()
            .HasForeignKey(o => o.GraveId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
