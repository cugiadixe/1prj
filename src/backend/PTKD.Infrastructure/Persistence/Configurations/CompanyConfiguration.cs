using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.CompanyCode)
            .HasColumnName("company_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.ParentCompanyId)
            .HasColumnName("parent_company_id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.TaxCode)
            .HasColumnName("tax_code")
            .HasMaxLength(50);

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(c => c.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion()
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(c => c.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.HasIndex(c => c.CompanyCode)
            .IsUnique()
            .HasDatabaseName("UQ_Companies_company_code");

        builder.HasOne(c => c.ParentCompany)
            .WithMany(c => c.ChildCompanies)
            .HasForeignKey(c => c.ParentCompanyId)
            .HasConstraintName("FK_Companies_parent_company_id");
            
        // EF Core mapping for NoDirectSelfParent CK is not strictly necessary for reads/writes if enforced in SQL, 
        // but we respect the schema mappings.
    }
}
