using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class WorkflowConditionConfiguration : IEntityTypeConfiguration<WorkflowCondition>
{
    public void Configure(EntityTypeBuilder<WorkflowCondition> builder)
    {
        builder.ToTable("Workflow_Conditions");

        builder.HasKey(e => e.Id).HasName("PK_Workflow_Conditions");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.WorkflowVersionId).HasColumnName("workflow_version_id").IsRequired();
        builder.Property(e => e.FieldCode).HasColumnName("field_code").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Operator).HasColumnName("operator").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Value).HasColumnName("value").HasMaxLength(1000).IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne(e => e.Version)
            .WithMany(v => v.Conditions)
            .HasForeignKey(e => e.WorkflowVersionId)
            .HasConstraintName("FK_WC_workflow_version_id");
    }
}
