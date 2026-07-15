using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class UserDepartmentAssignmentConfiguration : IEntityTypeConfiguration<UserDepartmentAssignment>
{
    public void Configure(EntityTypeBuilder<UserDepartmentAssignment> builder)
    {
        builder.ToTable("User_Department_Assignments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(a => a.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(a => a.UserCompanyAssignmentId)
            .HasColumnName("user_company_assignment_id")
            .IsRequired();

        builder.Property(a => a.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(a => a.IsPrimaryForCompany)
            .HasColumnName("is_primary_for_company")
            .IsRequired();

        builder.Property(a => a.AssignmentStatus)
            .HasColumnName("assignment_status")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.EffectiveFrom)
            .HasColumnName("effective_from")
            .IsRequired();

        builder.Property(a => a.EffectiveTo)
            .HasColumnName("effective_to");

        builder.Property(a => a.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion()
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(a => a.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(a => a.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.HasIndex(a => new { a.UserId, a.DepartmentId })
            .IsUnique()
            .HasFilter("assignment_status = 'ACTIVE'")
            .HasDatabaseName("UQ_User_Dept_Active");

        builder.HasIndex(a => new { a.UserId, a.CompanyId })
            .IsUnique()
            .HasFilter("assignment_status = 'ACTIVE' AND is_primary_for_company = 1")
            .HasDatabaseName("UQ_User_Company_Primary_Dept");

        builder.HasOne(a => a.User)
            .WithMany(u => u.DepartmentAssignments)
            .HasForeignKey(a => a.UserId)
            .HasConstraintName("FK_UserDepartmentAssignments_user_id");

        // The exact composite foreign keys mapped here may be simpler on the Entity object side,
        // but EF Core can map them if we specify principal keys correctly.
        // For simplicity and to avoid overly complex dependent loading, we map standard navigation:
        builder.HasOne(a => a.Department)
            .WithMany(d => d.UserAssignments)
            .HasForeignKey(a => a.DepartmentId)
            .HasPrincipalKey(d => d.Id) // Mapping simply to ID. The DB schema uses composite FK.
            .HasConstraintName("FK_UserDepartmentAssignments_department_id_company_id");

        builder.HasOne(a => a.UserCompanyAssignment)
            .WithMany(ca => ca.DepartmentAssignments)
            .HasForeignKey(a => a.UserCompanyAssignmentId)
            .HasPrincipalKey(ca => ca.Id) // Simple FK on object model.
            .HasConstraintName("FK_UserDepartmentAssignments_company_assignment");
    }
}
