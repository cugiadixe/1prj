using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CarePackageRequestItemConfiguration : IEntityTypeConfiguration<CarePackageRequestItem>
{
    public void Configure(EntityTypeBuilder<CarePackageRequestItem> builder)
    {
        builder.ToTable("Care_Package_Request_Items", "dbo");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CarePackageRequestId).HasColumnName("care_package_request_id");
        builder.Property(e => e.GraveId).HasColumnName("grave_id").HasMaxLength(100);
        builder.Property(e => e.CotCountSnapshot).HasColumnName("cot_count_snapshot");
        builder.Property(e => e.ServicePeriodStartDate).HasColumnName("service_period_start_date");
        builder.Property(e => e.ServicePeriodEndDate).HasColumnName("service_period_end_date");
        builder.Property(e => e.UnitPriceSnapshot).HasColumnName("unit_price_snapshot").HasColumnType("decimal(18,2)");
        builder.Property(e => e.LineSubtotal).HasColumnName("line_subtotal").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    }
}
