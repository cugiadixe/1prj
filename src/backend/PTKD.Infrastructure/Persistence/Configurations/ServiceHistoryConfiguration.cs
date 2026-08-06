using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class ServiceHistoryConfiguration : IEntityTypeConfiguration<ServiceHistory>
{
    public void Configure(EntityTypeBuilder<ServiceHistory> builder)
    {
        builder.ToTable("Service_History");

        builder.HasKey(e => e.Id).HasName("PK_Service_History");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServiceId).HasColumnName("service_id").IsRequired();
        builder.Property(e => e.ActionCode).HasColumnName("action_code").HasMaxLength(30).IsRequired();
        builder.Property(e => e.BeforeData).HasColumnName("before_data");
        builder.Property(e => e.AfterData).HasColumnName("after_data");
        builder.Property(e => e.ActedByUserId).HasColumnName("acted_by_user_id").IsRequired();
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(e => e.ServiceId).HasDatabaseName("IX_SH_service_id");
        builder.HasIndex(e => new { e.ServiceId, e.CreatedAt }).HasDatabaseName("IX_SH_service_created");

        builder.HasOne<Service>()
            .WithMany()
            .HasForeignKey(e => e.ServiceId)
            .HasConstraintName("FK_SH_service_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
