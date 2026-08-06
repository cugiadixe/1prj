using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class WorkflowStepApproverRuleConfiguration : IEntityTypeConfiguration<WorkflowStepApproverRule>
{
    public void Configure(EntityTypeBuilder<WorkflowStepApproverRule> builder)
    {
        builder.ToTable("Workflow_Step_Approver_Rules");

        builder.HasKey(e => e.Id).HasName("PK_Workflow_Step_Approver_Rules");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.WorkflowStepId).HasColumnName("workflow_step_id").IsRequired();
        builder.Property(e => e.ApproverSourceType).HasColumnName("approver_source_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.ApproverSourceValue).HasColumnName("approver_source_value").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Priority).HasColumnName("priority").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne(e => e.Step)
            .WithMany(s => s.ApproverRules)
            .HasForeignKey(e => e.WorkflowStepId)
            .HasConstraintName("FK_WSAR_workflow_step_id");
    }
}
