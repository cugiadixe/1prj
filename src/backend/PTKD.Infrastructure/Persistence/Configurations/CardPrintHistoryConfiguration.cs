using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CardPrintHistoryConfiguration : IEntityTypeConfiguration<CardPrintHistory>
{
    public void Configure(EntityTypeBuilder<CardPrintHistory> builder)
    {
        builder.ToTable("Card_Print_History", "dbo");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.CardId).HasColumnName("card_id");
        builder.Property(h => h.CompanyId).HasColumnName("company_id");
        builder.Property(h => h.PrintSequence).HasColumnName("print_sequence");
        builder.Property(h => h.PrintType).HasColumnName("print_type").HasMaxLength(20);
        builder.Property(h => h.ReprintRequestId).HasColumnName("reprint_request_id");
        builder.Property(h => h.WorkflowInstanceId).HasColumnName("workflow_instance_id");
        builder.Property(h => h.PrintedByUserId).HasColumnName("printed_by_user_id");
        builder.Property(h => h.PrintedAt).HasColumnName("printed_at");
        builder.Property(h => h.ReasonCode).HasColumnName("reason_code").HasMaxLength(50);
        builder.Property(h => h.Notes).HasColumnName("notes").HasMaxLength(500);
    }
}
