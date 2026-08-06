using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class UserCompanyAssignmentConfiguration : IEntityTypeConfiguration<UserCompanyAssignment>
{
    public void Configure(EntityTypeBuilder<UserCompanyAssignment> builder)
    {
        builder.ToTable("User_Company_Assignments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(a => a.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(a => a.IsPrimary)
            .HasColumnName("is_primary")
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

        builder.HasIndex(a => new { a.Id, a.UserId, a.CompanyId })
            .IsUnique()
            .HasDatabaseName("UQ_UserCompanyAssignments_Id_UserId_CompanyId");

        builder.HasIndex(a => new { a.UserId, a.CompanyId })
            .IsUnique()
            .HasFilter("assignment_status = 'ACTIVE'")
            .HasDatabaseName("UQ_User_Company_Active");

        builder.HasIndex(a => a.UserId)
            .IsUnique()
            .HasFilter("assignment_status = 'ACTIVE' AND is_primary = 1")
            .HasDatabaseName("UQ_User_Primary_Company");

        builder.HasOne(a => a.User)
            .WithMany(u => u.CompanyAssignments)
            .HasForeignKey(a => a.UserId)
            .HasConstraintName("FK_UserCompanyAssignments_user_id");

        builder.HasOne(a => a.Company)
            .WithMany(c => c.UserAssignments)
            .HasForeignKey(a => a.CompanyId)
            .HasConstraintName("FK_UserCompanyAssignments_company_id");
    }
}
