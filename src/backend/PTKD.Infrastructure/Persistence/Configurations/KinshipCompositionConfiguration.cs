using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class KinshipCompositionConfiguration : IEntityTypeConfiguration<KinshipComposition>
{
    public void Configure(EntityTypeBuilder<KinshipComposition> builder)
    {
        builder.ToTable("Kinship_Composition", "dbo");
        builder.HasKey(k => new { k.KindA, k.KindB, k.PivotGender });

        builder.Property(k => k.KindA).HasColumnName("kind_a").HasMaxLength(24);
        builder.Property(k => k.KindB).HasColumnName("kind_b").HasMaxLength(24);
        builder.Property(k => k.PivotGender).HasColumnName("pivot_gender").HasMaxLength(10);
        builder.Property(k => k.ResultKind).HasColumnName("result_kind").HasMaxLength(24);
        builder.Property(k => k.NeedsConfirmation).HasColumnName("needs_confirmation");
        builder.Property(k => k.Note).HasColumnName("note").HasMaxLength(200);
    }
}
