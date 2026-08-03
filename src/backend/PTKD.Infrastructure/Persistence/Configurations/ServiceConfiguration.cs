using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");

        builder.HasKey(e => e.Id).HasName("PK_Services");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.ServiceTypeId).HasColumnName("service_type_id").IsRequired();
        builder.Property(e => e.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(e => e.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(e => e.AppliedPrice).HasColumnName("applied_price").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.StandardPriceSnapshot).HasColumnName("standard_price_snapshot").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.IsOverridePrice).HasColumnName("is_override_price").IsRequired();
        builder.Property(e => e.OverrideApprovalRequestId).HasColumnName("override_approval_request_id");
        builder.Property(e => e.ValidFrom).HasColumnName("valid_from").IsRequired();
        builder.Property(e => e.ValidTo).HasColumnName("valid_to");
        builder.Property(e => e.CycleNumber).HasColumnName("cycle_number").IsRequired();
        builder.Property(e => e.PreviousServiceId).HasColumnName("previous_service_id");
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();

        builder.HasIndex(e => new { e.CustomerId, e.CompanyId }).HasDatabaseName("IX_Services_customer_company");
        builder.HasIndex(e => new { e.CompanyId, e.Status }).HasDatabaseName("IX_Services_company_status");
        builder.HasIndex(e => e.ServiceTypeId).HasDatabaseName("IX_Services_service_type");
        builder.HasIndex(e => e.PreviousServiceId)
            .HasDatabaseName("IX_Services_previous")
            .HasFilter("[previous_service_id] IS NOT NULL");

        builder.HasOne<ServiceType>()
            .WithMany()
            .HasForeignKey(e => e.ServiceTypeId)
            .HasConstraintName("FK_Services_service_type_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .HasConstraintName("FK_Services_customer_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .HasConstraintName("FK_Services_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Service>()
            .WithMany()
            .HasForeignKey(e => e.PreviousServiceId)
            .HasConstraintName("FK_Services_previous_service_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
