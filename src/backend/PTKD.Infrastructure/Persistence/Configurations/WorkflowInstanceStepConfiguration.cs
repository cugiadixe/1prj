using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class WorkflowInstanceStepConfiguration : IEntityTypeConfiguration<WorkflowInstanceStep>
{
    public void Configure(EntityTypeBuilder<WorkflowInstanceStep> builder)
    {
        builder.ToTable("Workflow_Instance_Steps");

        builder.HasKey(e => e.Id).HasName("PK_Workflow_Instance_Steps");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.WorkflowInstanceId).HasColumnName("workflow_instance_id").IsRequired();
        builder.Property(e => e.WorkflowStepId).HasColumnName("workflow_step_id").IsRequired();
        builder.Property(e => e.StepOrder).HasColumnName("step_order").IsRequired();
        builder.Property(e => e.StepName).HasColumnName("step_name").HasMaxLength(500).IsRequired();
        builder.Property(e => e.RoundNo).HasColumnName("round_no").IsRequired();
        builder.Property(e => e.StepStatus).HasColumnName("step_status").HasMaxLength(20).IsRequired();
        builder.Property(e => e.IsOverdue).HasColumnName("is_overdue").IsRequired();
        builder.Property(e => e.AssignedAt).HasColumnName("assigned_at");
        builder.Property(e => e.CompletedAt).HasColumnName("completed_at");
        builder.Property(e => e.CompletedBy).HasColumnName("completed_by");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();

        builder.HasIndex(e => new { e.WorkflowInstanceId, e.RoundNo, e.StepOrder })
            .HasDatabaseName("IX_WIS_instance_round");

        builder.HasOne(e => e.Instance)
            .WithMany(i => i.Steps)
            .HasForeignKey(e => e.WorkflowInstanceId)
            .HasConstraintName("FK_WIS_workflow_instance_id");
    }
}
