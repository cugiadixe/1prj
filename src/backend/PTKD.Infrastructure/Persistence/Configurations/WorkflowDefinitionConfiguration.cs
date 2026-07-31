using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("Workflow_Definitions");

        builder.HasKey(e => e.Id).HasName("PK_Workflow_Definitions");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.DefinitionCode).HasColumnName("definition_code").HasMaxLength(100).IsRequired();
        builder.Property(e => e.DefinitionName).HasColumnName("definition_name").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(e => e.ProcessCode).HasColumnName("process_code").HasMaxLength(100).IsRequired();
        builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();

        builder.HasIndex(e => e.DefinitionCode).IsUnique().HasDatabaseName("UQ_Workflow_Definitions_definition_code");
        builder.HasIndex(e => e.ProcessCode).HasDatabaseName("IX_Workflow_Definitions_process_code");

        builder.HasOne(e => e.Process)
            .WithMany()
            .HasForeignKey(e => e.ProcessCode)
            .HasConstraintName("FK_Workflow_Definitions_process_code");
    }
}
