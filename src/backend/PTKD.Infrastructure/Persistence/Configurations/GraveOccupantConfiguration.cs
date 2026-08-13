using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class GraveOccupantConfiguration : IEntityTypeConfiguration<GraveOccupant>
{
    public void Configure(EntityTypeBuilder<GraveOccupant> builder)
    {
        builder.ToTable("Grave_Occupants", "dbo");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.GraveId).HasColumnName("grave_id");
        builder.Property(o => o.DeceasedCustomerId).HasColumnName("deceased_customer_id");
        builder.Property(o => o.FullName).HasColumnName("full_name").HasMaxLength(200);
        builder.Property(o => o.Gender).HasColumnName("gender").HasMaxLength(10);
        builder.Property(o => o.Dob).HasColumnName("dob");
        builder.Property(o => o.DeathDateSolar).HasColumnName("death_date_solar");
        builder.Property(o => o.DeathDateLunar).HasColumnName("death_date_lunar").HasMaxLength(20);
        builder.Property(o => o.BurialDate).HasColumnName("burial_date");
        builder.Property(o => o.Hometown).HasColumnName("hometown").HasMaxLength(200);
        builder.Property(o => o.OwnerRelationship).HasColumnName("owner_relationship").HasMaxLength(100);
        builder.Property(o => o.DeceasedRelationship).HasColumnName("deceased_relationship").HasMaxLength(100);
        builder.Property(o => o.Notes).HasColumnName("notes").HasMaxLength(2000);
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        builder.Property(o => o.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(o => o.RowVersion).HasColumnName("row_version").IsRowVersion();
    }
}
