using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CustomerRelationshipConfiguration : IEntityTypeConfiguration<CustomerRelationship>
{
    public void Configure(EntityTypeBuilder<CustomerRelationship> builder)
    {
        builder.ToTable("Customer_Relationships", "dbo");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.FromCustomerId).HasColumnName("from_customer_id");
        builder.Property(r => r.ToCustomerId).HasColumnName("to_customer_id");
        builder.Property(r => r.RelationKind).HasColumnName("relation_kind").HasMaxLength(24);
        builder.Property(r => r.IsDerived).HasColumnName("is_derived");
        builder.Property(r => r.NeedsConfirmation).HasColumnName("needs_confirmation");
        builder.Property(r => r.Note).HasColumnName("note").HasMaxLength(500);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(r => r.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasIndex(r => new { r.FromCustomerId, r.ToCustomerId }).IsUnique();
    }
}
