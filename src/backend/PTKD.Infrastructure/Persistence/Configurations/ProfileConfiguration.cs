using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("Profiles");

        builder.HasKey(p => p.Id).HasName("PK_Profiles");
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.Cccd).HasColumnName("cccd").HasMaxLength(20);
        builder.Property(p => p.Dob).HasColumnName("dob");
        builder.Property(p => p.DobPartial).HasColumnName("dob_partial").HasMaxLength(10);
        builder.Property(p => p.DobPrecision).HasColumnName("dob_precision").HasMaxLength(10);
        builder.Property(p => p.Gender).HasColumnName("gender").HasMaxLength(10);
        builder.Property(p => p.PermanentAddress).HasColumnName("permanent_address").HasMaxLength(500);
        builder.Property(p => p.CccdIssueDate).HasColumnName("cccd_issue_date");
        builder.Property(p => p.CccdIssuePlace).HasColumnName("cccd_issue_place").HasMaxLength(200);
        builder.Property(p => p.TaxCode).HasColumnName("tax_code").HasMaxLength(20);
        builder.Property(p => p.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(p => p.ContactAddress).HasColumnName("contact_address").HasMaxLength(500);
        builder.Property(p => p.DeathDateSolar).HasColumnName("death_date_solar");
        builder.Property(p => p.DeathDateLunar).HasColumnName("death_date_lunar").HasMaxLength(20);
        builder.Property(p => p.DeathPlace).HasColumnName("death_place").HasMaxLength(200);
        builder.Property(p => p.Hometown).HasColumnName("hometown").HasMaxLength(200);
        builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(p => p.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedByUserId).HasColumnName("updated_by_user_id");
    }
}
