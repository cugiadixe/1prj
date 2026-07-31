using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class WorkflowBindingConfiguration : IEntityTypeConfiguration<WorkflowBinding>
{
    public void Configure(EntityTypeBuilder<WorkflowBinding> builder)
    {
        builder.ToTable("Workflow_Bindings");

        builder.HasKey(e => e.Id).HasName("PK_Workflow_Bindings");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.WorkflowVersionId).HasColumnName("workflow_version_id").IsRequired();
        builder.Property(e => e.ProcessCode).HasColumnName("process_code").HasMaxLength(100).IsRequired();
        builder.Property(e => e.ScopeType).HasColumnName("scope_type").HasMaxLength(20).IsRequired();
        builder.Property(e => e.CompanyId).HasColumnName("company_id");
        builder.Property(e => e.Priority).HasColumnName("priority").IsRequired();
        builder.Property(e => e.EffectiveFrom).HasColumnName("effective_from").IsRequired();
        builder.Property(e => e.EffectiveTo).HasColumnName("effective_to");
        builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();

        builder.HasIndex(e => new { e.ProcessCode, e.ScopeType, e.CompanyId })
            .HasDatabaseName("IX_WB_process_scope")
            .HasFilter("[is_active] = 1");

        builder.HasOne(e => e.Version)
            .WithMany()
            .HasForeignKey(e => e.WorkflowVersionId)
            .HasConstraintName("FK_WB_workflow_version_id");

        builder.HasOne(e => e.Process)
            .WithMany()
            .HasForeignKey(e => e.ProcessCode)
            .HasConstraintName("FK_WB_process_code");
    }
}
