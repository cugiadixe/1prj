using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("Payment_Transactions");

        builder.HasKey(e => e.Id).HasName("PK_Payment_Transactions");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.BillCode).HasColumnName("bill_code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(e => e.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(20).IsRequired();
        builder.Property(e => e.PaymentDate).HasColumnName("payment_date").IsRequired();
        builder.Property(e => e.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(e => e.ConfirmedAt).HasColumnName("confirmed_at");
        builder.Property(e => e.ConfirmedByUserId).HasColumnName("confirmed_by_user_id");
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired();
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();

        builder.HasIndex(e => new { e.CompanyId, e.BillCode }).IsUnique().HasDatabaseName("UQ_Payment_Transactions_bill_code");
        builder.HasIndex(e => e.CompanyId).HasDatabaseName("IX_PT_company_id");
        builder.HasIndex(e => e.CustomerId).HasDatabaseName("IX_PT_customer_id");
        builder.HasIndex(e => new { e.CompanyId, e.Status }).HasDatabaseName("IX_PT_company_status");
        builder.HasIndex(e => new { e.CompanyId, e.PaymentDate }).HasDatabaseName("IX_PT_company_payment_date");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .HasConstraintName("FK_PT_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .HasConstraintName("FK_PT_customer_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.ConfirmedByUserId)
            .HasConstraintName("FK_PT_confirmed_by_user_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .HasConstraintName("FK_PT_created_by_user_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Items)
            .WithOne()
            .HasForeignKey(e => e.PaymentTransactionId)
            .HasConstraintName("FK_PTI_payment_transaction_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
