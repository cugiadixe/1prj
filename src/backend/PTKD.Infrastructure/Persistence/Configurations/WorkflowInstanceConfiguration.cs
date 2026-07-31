using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("Workflow_Instances");

        builder.HasKey(e => e.Id).HasName("PK_Workflow_Instances");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.WorkflowVersionId).HasColumnName("workflow_version_id").IsRequired();
        builder.Property(e => e.WorkflowBindingId).HasColumnName("workflow_binding_id").IsRequired();
        builder.Property(e => e.ProcessCode).HasColumnName("process_code").HasMaxLength(100).IsRequired();
        builder.Property(e => e.CompanyId).HasColumnName("company_id");
        builder.Property(e => e.RequesterId).HasColumnName("requester_id").IsRequired();
        builder.Property(e => e.BusinessEntityType).HasColumnName("business_entity_type").HasMaxLength(100).IsRequired();
        builder.Property(e => e.BusinessEntityId).HasColumnName("business_entity_id").IsRequired();
        builder.Property(e => e.InstanceStatus).HasColumnName("instance_status").HasMaxLength(30).IsRequired();
        builder.Property(e => e.RoundNo).HasColumnName("round_no").IsRequired();
        builder.Property(e => e.WorkflowSnapshotJson).HasColumnName("workflow_snapshot_json").IsRequired();
        builder.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
        builder.Property(e => e.PayloadHash).HasColumnName("payload_hash").HasMaxLength(128).IsRequired();
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(e => e.BeforeDataJson).HasColumnName("before_data_json");
        builder.Property(e => e.AfterDataJson).HasColumnName("after_data_json");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();

        builder.HasIndex(e => new { e.RequesterId, e.InstanceStatus }).HasDatabaseName("IX_WI_requester");
        builder.HasIndex(e => new { e.BusinessEntityType, e.BusinessEntityId }).HasDatabaseName("IX_WI_business_entity");
        builder.HasIndex(e => new { e.ProcessCode, e.CompanyId, e.InstanceStatus }).HasDatabaseName("IX_WI_process_company");
    }
}
