using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class RelationshipKindConfiguration : IEntityTypeConfiguration<RelationshipKind>
{
    public void Configure(EntityTypeBuilder<RelationshipKind> builder)
    {
        builder.ToTable("Relationship_Kinds", "dbo");
        builder.HasKey(k => k.KindCode);

        builder.Property(k => k.KindCode).HasColumnName("kind_code").HasMaxLength(24);
        builder.Property(k => k.LabelMale).HasColumnName("label_male").HasMaxLength(50);
        builder.Property(k => k.LabelFemale).HasColumnName("label_female").HasMaxLength(50);
        builder.Property(k => k.LabelNeutral).HasColumnName("label_neutral").HasMaxLength(50);
        builder.Property(k => k.InverseCode).HasColumnName("inverse_code").HasMaxLength(24);
        builder.Property(k => k.IsSymmetric).HasColumnName("is_symmetric");
        builder.Property(k => k.SortOrder).HasColumnName("sort_order");
    }
}
