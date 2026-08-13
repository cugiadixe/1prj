using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class ApprovalAuthorityConfiguration : IEntityTypeConfiguration<ApprovalAuthority>
{
    public void Configure(EntityTypeBuilder<ApprovalAuthority> builder)
    {
        builder.ToTable("Approval_Authorities");

        builder.HasKey(e => e.Id).HasName("PK_Approval_Authorities");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(e => e.DepartmentId).HasColumnName("department_id").IsRequired();
        builder.Property(e => e.ProcessCode).HasColumnName("process_code").HasMaxLength(100);
        builder.Property(e => e.ApproverUserId).HasColumnName("approver_user_id").IsRequired();
        builder.Property(e => e.AuthorityLevel).HasColumnName("authority_level").IsRequired();
        builder.Property(e => e.MinAmount).HasColumnName("min_amount").HasColumnType("decimal(18,2)");
        builder.Property(e => e.MaxAmount).HasColumnName("max_amount").HasColumnType("decimal(18,2)");
        builder.Property(e => e.EffectiveFrom).HasColumnName("effective_from").IsRequired();
        builder.Property(e => e.EffectiveTo).HasColumnName("effective_to");
        builder.Property(e => e.DelegatedFromUserId).HasColumnName("delegated_from_user_id");
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(2000);
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

        // Chỉ mục phục vụ resolver: tra theo (công ty, phòng ban, cấp) trên các dòng đang hiệu lực.
        builder.HasIndex(e => new { e.CompanyId, e.DepartmentId, e.AuthorityLevel, e.Status })
            .HasDatabaseName("IX_AA_company_department_level_status");
    }
}
