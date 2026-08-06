using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CustomerChangeRequestConfiguration : IEntityTypeConfiguration<CustomerChangeRequest>
{
    public void Configure(EntityTypeBuilder<CustomerChangeRequest> builder)
    {
        builder.ToTable("Customer_Change_Requests");

        builder.HasKey(e => e.Id).HasName("PK_Customer_Change_Requests");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ProcessCode).HasColumnName("process_code").HasMaxLength(100).IsRequired();
        builder.Property(e => e.RequesterId).HasColumnName("requester_id").IsRequired();
        builder.Property(e => e.CompanyId).HasColumnName("company_id");
        builder.Property(e => e.RequestStatus).HasColumnName("request_status").HasMaxLength(30).IsRequired();
        builder.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
        builder.Property(e => e.WorkflowInstanceId).HasColumnName("workflow_instance_id");
        builder.Property(e => e.CreatedCustomerId).HasColumnName("created_customer_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();

        builder.Property(e => e.TargetCustomerId).HasColumnName("target_customer_id");
        builder.Property(e => e.TargetRowVersion).HasColumnName("target_row_version");

        builder.HasIndex(e => new { e.RequesterId, e.RequestStatus }).HasDatabaseName("IX_CCR_requester");
        builder.HasIndex(e => e.WorkflowInstanceId).HasDatabaseName("IX_CCR_workflow_instance")
            .HasFilter("[workflow_instance_id] IS NOT NULL");
        builder.HasIndex(e => e.TargetCustomerId).HasDatabaseName("IX_CCR_target_customer")
            .HasFilter("[target_customer_id] IS NOT NULL");
    }
}
