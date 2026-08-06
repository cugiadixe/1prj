using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.DepartmentCode)
            .HasColumnName("department_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(d => d.ParentDepartmentId)
            .HasColumnName("parent_department_id");

        builder.Property(d => d.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(d => d.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion()
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(d => d.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(d => d.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.HasIndex(d => d.DepartmentCode)
            .IsUnique()
            .HasDatabaseName("UQ_Departments_department_code");

        builder.HasIndex(d => new { d.Id, d.CompanyId })
            .IsUnique()
            .HasDatabaseName("UQ_Departments_Id_CompanyId");

        builder.HasOne(d => d.Company)
            .WithMany(c => c.Departments)
            .HasForeignKey(d => d.CompanyId)
            .HasConstraintName("FK_Departments_company_id");

        builder.HasOne(d => d.ParentDepartment)
            .WithMany(d => d.ChildDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .HasPrincipalKey(d => d.Id) // Custom principal mapping typically requires exact match of FK properties
            // The SQL constraint uses FK_Departments_parent_department_id (parent_department_id, company_id) REFERENCES Departments(id, company_id)
            // But EF Core navigation mapping doesn't strictly need the composite FK here unless we want to enforce it at the navigation level.
            // We'll leave it as a simple FK for the object model since CompanyId shouldn't change.
            .HasConstraintName("FK_Departments_parent_department_id");
    }
}
