using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class WorkflowInstanceStepAssigneeConfiguration : IEntityTypeConfiguration<WorkflowInstanceStepAssignee>
{
    public void Configure(EntityTypeBuilder<WorkflowInstanceStepAssignee> builder)
    {
        builder.ToTable("Workflow_Instance_Step_Assignees");

        builder.HasKey(e => e.Id).HasName("PK_Workflow_Instance_Step_Assignees");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.WorkflowInstanceStepId).HasColumnName("workflow_instance_step_id").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.ApproverSourceType).HasColumnName("approver_source_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.IsResolved).HasColumnName("is_resolved").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(e => new { e.WorkflowInstanceStepId, e.UserId })
            .IsUnique()
            .HasDatabaseName("UQ_WISA_step_user");

        builder.HasOne(e => e.Step)
            .WithMany(s => s.Assignees)
            .HasForeignKey(e => e.WorkflowInstanceStepId)
            .HasConstraintName("FK_WISA_workflow_instance_step_id");
    }
}
