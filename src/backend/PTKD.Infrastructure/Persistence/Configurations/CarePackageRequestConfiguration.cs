using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CarePackageRequestConfiguration : IEntityTypeConfiguration<CarePackageRequest>
{
    public void Configure(EntityTypeBuilder<CarePackageRequest> builder)
    {
        builder.ToTable("Care_Package_Requests", "dbo");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CompanyId).HasColumnName("company_id");
        builder.Property(e => e.CustomerId).HasColumnName("customer_id");
        builder.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(50);
        builder.Property(e => e.RequiresApproval).HasColumnName("requires_approval");
        builder.Property(e => e.WorkflowInstanceId).HasColumnName("workflow_instance_id");
        builder.Property(e => e.ServiceId).HasColumnName("service_id");
        builder.Property(e => e.SaleDate).HasColumnName("sale_date");
        builder.Property(e => e.SubtotalAmount).HasColumnName("subtotal_amount").HasColumnType("decimal(18,2)");
        builder.Property(e => e.DiscountAmount).HasColumnName("discount_amount").HasColumnType("decimal(18,2)");
        builder.Property(e => e.DiscountReason).HasColumnName("discount_reason").HasMaxLength(500);
        builder.Property(e => e.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(18,2)");
        builder.Property(e => e.PaymentTransactionId).HasColumnName("payment_transaction_id");
        builder.Property(e => e.PreviousRequestId).HasColumnName("previous_request_id");
        
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");
        
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasMany(e => e.Items)
            .WithOne(i => i.CarePackageRequest)
            .HasForeignKey(i => i.CarePackageRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
