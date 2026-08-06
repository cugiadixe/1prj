using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class EmploymentHistoryConfiguration : IEntityTypeConfiguration<EmploymentHistory>
{
    public void Configure(EntityTypeBuilder<EmploymentHistory> builder)
    {
        builder.ToTable("Employment_Histories");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");

        builder.Property(h => h.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(h => h.FromCompanyId).HasColumnName("from_company_id");
        builder.Property(h => h.ToCompanyId).HasColumnName("to_company_id");
        builder.Property(h => h.FromDepartmentId).HasColumnName("from_department_id");
        builder.Property(h => h.ToDepartmentId).HasColumnName("to_department_id");
        builder.Property(h => h.FromCompanyAssignmentId).HasColumnName("from_company_assignment_id");
        builder.Property(h => h.ToCompanyAssignmentId).HasColumnName("to_company_assignment_id");
        builder.Property(h => h.FromDepartmentAssignmentId).HasColumnName("from_department_assignment_id");
        builder.Property(h => h.ToDepartmentAssignmentId).HasColumnName("to_department_assignment_id");

        builder.Property(h => h.ActionType)
            .HasColumnName("action_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(h => h.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500);

        builder.Property(h => h.EffectiveDate)
            .HasColumnName("effective_date")
            .IsRequired();

        builder.Property(h => h.CorrelationId)
            .HasColumnName("correlation_id");

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(h => h.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.HasOne(h => h.User)
            .WithMany(u => u.EmploymentHistories)
            .HasForeignKey(h => h.UserId)
            .HasConstraintName("FK_EmploymentHistories_user_id");
    }
}
