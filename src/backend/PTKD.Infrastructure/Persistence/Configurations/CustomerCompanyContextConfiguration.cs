using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CustomerCompanyContextConfiguration : IEntityTypeConfiguration<CustomerCompanyContext>
{
    public void Configure(EntityTypeBuilder<CustomerCompanyContext> builder)
    {
        builder.ToTable("Customer_Company_Contexts");

        builder.HasKey(c => c.Id).HasName("PK_Customer_Company_Contexts");
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(c => c.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(c => c.AssignedStaffId).HasColumnName("assigned_staff_id");
        builder.Property(c => c.RelationshipStatus).HasColumnName("relationship_status").HasMaxLength(20).IsRequired();
        builder.Property(c => c.InternalNotes).HasColumnName("internal_notes").HasMaxLength(2000);
        builder.Property(c => c.FirstInteractionAt).HasColumnName("first_interaction_at");
        builder.Property(c => c.LastInteractionAt).HasColumnName("last_interaction_at");
        builder.Property(c => c.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.UpdatedByUserId).HasColumnName("updated_by_user_id");

        builder.HasIndex(c => new { c.CustomerId, c.CompanyId })
            .IsUnique()
            .HasDatabaseName("UQ_Customer_Company_Contexts_customer_company");
    }
}
