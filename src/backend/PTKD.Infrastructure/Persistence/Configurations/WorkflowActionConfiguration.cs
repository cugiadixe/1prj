using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class WorkflowActionConfiguration : IEntityTypeConfiguration<WorkflowAction>
{
    public void Configure(EntityTypeBuilder<WorkflowAction> builder)
    {
        builder.ToTable("Workflow_Actions");

        builder.HasKey(e => e.Id).HasName("PK_Workflow_Actions");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.WorkflowInstanceStepId).HasColumnName("workflow_instance_step_id").IsRequired();
        builder.Property(e => e.WorkflowInstanceId).HasColumnName("workflow_instance_id").IsRequired();
        builder.Property(e => e.ActionType).HasColumnName("action_type").HasMaxLength(20).IsRequired();
        builder.Property(e => e.ActedBy).HasColumnName("acted_by").IsRequired();
        builder.Property(e => e.OnBehalfOf).HasColumnName("on_behalf_of");
        builder.Property(e => e.DelegationId).HasColumnName("delegation_id");
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(2000);
        builder.Property(e => e.Comment).HasColumnName("comment").HasMaxLength(4000);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(e => e.WorkflowInstanceStepId).HasDatabaseName("IX_WA_instance_step");
        builder.HasIndex(e => new { e.ActedBy, e.CreatedAt }).HasDatabaseName("IX_WA_acted_by").IsDescending(false, true);
    }
}
