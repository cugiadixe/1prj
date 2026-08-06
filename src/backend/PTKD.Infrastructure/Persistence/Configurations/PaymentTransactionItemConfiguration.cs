using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class PaymentTransactionItemConfiguration : IEntityTypeConfiguration<PaymentTransactionItem>
{
    public void Configure(EntityTypeBuilder<PaymentTransactionItem> builder)
    {
        builder.ToTable("Payment_Transaction_Items");

        builder.HasKey(e => e.Id).HasName("PK_Payment_Transaction_Items");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PaymentTransactionId).HasColumnName("payment_transaction_id").IsRequired();
        builder.Property(e => e.ServiceId).HasColumnName("service_id").IsRequired();
        builder.Property(e => e.ServiceTypeCode).HasColumnName("service_type_code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.ServiceCycleNumber).HasColumnName("service_cycle_number").IsRequired();
        builder.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(e => e.PaymentTransactionId).HasDatabaseName("IX_PTI_payment_transaction_id");
        builder.HasIndex(e => e.ServiceId).HasDatabaseName("IX_PTI_service_id");

        builder.HasOne<Service>()
            .WithMany()
            .HasForeignKey(e => e.ServiceId)
            .HasConstraintName("FK_PTI_service_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
