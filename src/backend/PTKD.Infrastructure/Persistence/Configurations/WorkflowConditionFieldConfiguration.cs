using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class WorkflowConditionFieldConfiguration : IEntityTypeConfiguration<WorkflowConditionField>
{
    public void Configure(EntityTypeBuilder<WorkflowConditionField> builder)
    {
        builder.ToTable("Workflow_Condition_Fields");

        builder.HasKey(e => e.Id).HasName("PK_Workflow_Condition_Fields");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ProcessCode).HasColumnName("process_code").HasMaxLength(100).IsRequired();
        builder.Property(e => e.FieldCode).HasColumnName("field_code").HasMaxLength(100).IsRequired();
        builder.Property(e => e.FieldLabel).HasColumnName("field_label").HasMaxLength(300).IsRequired();
        builder.Property(e => e.DataType).HasColumnName("data_type").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}
