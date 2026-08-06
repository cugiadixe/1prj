using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CardReprintRequestConfiguration : IEntityTypeConfiguration<CardReprintRequest>
{
    public void Configure(EntityTypeBuilder<CardReprintRequest> builder)
    {
        builder.ToTable("Card_Reprint_Requests", "dbo");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.CompanyId).HasColumnName("company_id");
        builder.Property(r => r.CardId).HasColumnName("card_id");
        builder.Property(r => r.RequesterId).HasColumnName("requester_id");
        builder.Property(r => r.RequestType).HasColumnName("request_type").HasMaxLength(50);
        builder.Property(r => r.ReprintNumber).HasColumnName("reprint_number");
        builder.Property(r => r.FeeAmount).HasColumnName("fee_amount");
        builder.Property(r => r.FeeCurrency).HasColumnName("fee_currency").HasMaxLength(3);
        builder.Property(r => r.ReasonCode).HasColumnName("reason_code").HasMaxLength(100);
        builder.Property(r => r.WorkflowInstanceId).HasColumnName("workflow_instance_id");
        builder.Property(r => r.PaymentTransactionId).HasColumnName("payment_transaction_id");
        builder.Property(r => r.ServiceItemId).HasColumnName("service_item_id");
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(50);
        builder.Property(r => r.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(r => r.PrintedAt).HasColumnName("printed_at");
        builder.Property(r => r.PrintedByUserId).HasColumnName("printed_by_user_id");
        builder.Property(r => r.ReleasedAt).HasColumnName("released_at");
        builder.Property(r => r.ReleasedByUserId).HasColumnName("released_by_user_id");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(r => r.RowVersion).HasColumnName("row_version").IsRowVersion();
    }
}
