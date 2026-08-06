using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.EmployeeCode)
            .HasColumnName("employee_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(200);

        builder.Property(u => u.EmploymentStatus)
            .HasColumnName("employment_status")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(u => u.AccountStatus)
            .HasColumnName("account_status")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(u => u.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion()
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(u => u.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.HasIndex(u => u.EmployeeCode)
            .IsUnique()
            .HasDatabaseName("UQ_Users_employee_code");
    }
}
