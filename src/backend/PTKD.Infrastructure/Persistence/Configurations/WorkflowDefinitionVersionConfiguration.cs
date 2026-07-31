using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class WorkflowDefinitionVersionConfiguration : IEntityTypeConfiguration<WorkflowDefinitionVersion>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinitionVersion> builder)
    {
        builder.ToTable("Workflow_Definition_Versions");

        builder.HasKey(e => e.Id).HasName("PK_Workflow_Definition_Versions");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.WorkflowDefinitionId).HasColumnName("workflow_definition_id").IsRequired();
        builder.Property(e => e.VersionNumber).HasColumnName("version_number").IsRequired();
        builder.Property(e => e.VersionStatus).HasColumnName("version_status").HasMaxLength(20).IsRequired();
        builder.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
        builder.Property(e => e.EffectiveTo).HasColumnName("effective_to");
        builder.Property(e => e.PublishedAt).HasColumnName("published_at");
        builder.Property(e => e.PublishedBy).HasColumnName("published_by");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();

        builder.HasIndex(e => new { e.WorkflowDefinitionId, e.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UQ_WDV_definition_version");

        builder.HasIndex(e => new { e.WorkflowDefinitionId, e.VersionStatus })
            .HasDatabaseName("IX_WDV_status");

        builder.HasOne(e => e.Definition)
            .WithMany(d => d.Versions)
            .HasForeignKey(e => e.WorkflowDefinitionId)
            .HasConstraintName("FK_WDV_workflow_definition_id");
    }
}
