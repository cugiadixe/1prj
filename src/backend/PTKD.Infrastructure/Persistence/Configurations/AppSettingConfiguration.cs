using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("App_Settings", "dbo");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.SettingKey).HasColumnName("setting_key").HasMaxLength(100);
        builder.Property(s => s.SettingValue).HasColumnName("setting_value").HasMaxLength(1000);
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
        builder.Property(s => s.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(s => s.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasIndex(s => s.SettingKey).IsUnique();
    }
}
