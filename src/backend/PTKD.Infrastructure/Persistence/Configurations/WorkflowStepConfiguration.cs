using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("Workflow_Steps");

        builder.HasKey(e => e.Id).HasName("PK_Workflow_Steps");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.WorkflowVersionId).HasColumnName("workflow_version_id").IsRequired();
        builder.Property(e => e.StepOrder).HasColumnName("step_order").IsRequired();
        builder.Property(e => e.StepName).HasColumnName("step_name").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(e => e.IsRequired).HasColumnName("is_required").IsRequired();
        builder.Property(e => e.DueDurationMinutes).HasColumnName("due_duration_minutes");
        builder.Property(e => e.ReminderBeforeMinutes).HasColumnName("reminder_before_minutes");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();

        builder.HasIndex(e => new { e.WorkflowVersionId, e.StepOrder })
            .IsUnique()
            .HasDatabaseName("UQ_WS_version_order");

        builder.HasOne(e => e.Version)
            .WithMany(v => v.Steps)
            .HasForeignKey(e => e.WorkflowVersionId)
            .HasConstraintName("FK_WS_workflow_version_id");
    }
}
