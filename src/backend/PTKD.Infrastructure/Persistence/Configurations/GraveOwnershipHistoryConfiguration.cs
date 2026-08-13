using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class GraveOwnershipHistoryConfiguration : IEntityTypeConfiguration<GraveOwnershipHistory>
{
    public void Configure(EntityTypeBuilder<GraveOwnershipHistory> builder)
    {
        builder.ToTable("Grave_Ownership_History", "dbo");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.GraveId).HasColumnName("grave_id");
        builder.Property(h => h.PreviousOwnerId).HasColumnName("previous_owner_id");
        builder.Property(h => h.NewOwnerId).HasColumnName("new_owner_id");
        builder.Property(h => h.TransferType).HasColumnName("transfer_type").HasMaxLength(20);
        builder.Property(h => h.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(h => h.TransferredAt).HasColumnName("transferred_at");
        builder.Property(h => h.TransferredByUserId).HasColumnName("transferred_by_user_id");
        builder.Property(h => h.RowVersion).HasColumnName("row_version").IsRowVersion();

        builder.HasIndex(h => h.GraveId);
    }
}
