using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class PaymentCorrectionHistoryConfiguration : IEntityTypeConfiguration<PaymentCorrectionHistory>
{
    public void Configure(EntityTypeBuilder<PaymentCorrectionHistory> builder)
    {
        builder.ToTable("Payment_Correction_History");

        builder.HasKey(e => e.Id).HasName("PK_Payment_Correction_History");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.PaymentTransactionId).HasColumnName("payment_transaction_id").IsRequired();
        builder.Property(e => e.CorrectedByUserId).HasColumnName("corrected_by_user_id").IsRequired();
        builder.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
        builder.Property(e => e.BeforeData).HasColumnName("before_data").IsRequired();
        builder.Property(e => e.AfterData).HasColumnName("after_data").IsRequired();
        builder.Property(e => e.CorrectedFields).HasColumnName("corrected_fields").HasMaxLength(500).IsRequired();
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").IsRequired();
        builder.Property(e => e.AffectedReconciliationPeriods).HasColumnName("affected_reconciliation_periods");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(e => e.PaymentTransactionId).HasDatabaseName("IX_PCH_payment_transaction_id");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_PCH_created_at");

        builder.HasOne<PaymentTransaction>()
            .WithMany()
            .HasForeignKey(e => e.PaymentTransactionId)
            .HasConstraintName("FK_PCH_payment_transaction_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.CorrectedByUserId)
            .HasConstraintName("FK_PCH_corrected_by_user_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
