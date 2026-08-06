using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class ServicePriceHistoryConfiguration : IEntityTypeConfiguration<ServicePriceHistory>
{
    public void Configure(EntityTypeBuilder<ServicePriceHistory> builder)
    {
        builder.ToTable("Service_Price_History");

        builder.HasKey(e => e.Id).HasName("PK_Service_Price_History");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServiceTypeId).HasColumnName("service_type_id").IsRequired();
        builder.Property(e => e.Price).HasColumnName("price").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.EffectiveFrom).HasColumnName("effective_from").IsRequired();
        builder.Property(e => e.EffectiveTo).HasColumnName("effective_to");
        builder.Property(e => e.ChangedByUserId).HasColumnName("changed_by_user_id").IsRequired();
        builder.Property(e => e.ChangeReason).HasColumnName("change_reason").HasMaxLength(500).IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(e => e.ServiceTypeId).HasDatabaseName("IX_SPH_service_type_id");
        builder.HasIndex(e => new { e.ServiceTypeId, e.EffectiveFrom }).HasDatabaseName("IX_SPH_service_type_effective");

        builder.HasOne<ServiceType>()
            .WithMany()
            .HasForeignKey(e => e.ServiceTypeId)
            .HasConstraintName("FK_SPH_service_type_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
