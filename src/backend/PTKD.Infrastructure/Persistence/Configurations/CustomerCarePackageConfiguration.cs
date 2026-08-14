using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CustomerCarePackageConfiguration : IEntityTypeConfiguration<CustomerCarePackage>
{
    public void Configure(EntityTypeBuilder<CustomerCarePackage> builder)
    {
        builder.ToTable("Customer_Care_Packages", "dbo");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.CustomerId).HasColumnName("customer_id");
        builder.Property(c => c.ServiceTypeId).HasColumnName("service_type_id");
        builder.Property(c => c.GraveId).HasColumnName("grave_id");
        builder.Property(c => c.CotCount).HasColumnName("cot_count");
        builder.Property(c => c.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(18,2)");
        builder.Property(c => c.TotalPrice).HasColumnName("total_price").HasColumnType("decimal(18,2)");
        builder.Property(c => c.StartDate).HasColumnName("start_date");
        builder.Property(c => c.EndDate).HasColumnName("end_date");
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(c => c.Notes).HasColumnName("notes").HasMaxLength(2000);
        builder.Property(c => c.WorkflowInstanceId).HasColumnName("workflow_instance_id");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(c => c.RowVersion).HasColumnName("row_version").IsRowVersion();
    }
}
