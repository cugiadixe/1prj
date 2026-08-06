using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class ReconciliationPeriodConfiguration : IEntityTypeConfiguration<ReconciliationPeriod>
{
    public void Configure(EntityTypeBuilder<ReconciliationPeriod> builder)
    {
        builder.ToTable("Reconciliation_Periods");

        builder.HasKey(e => e.Id).HasName("PK_Reconciliation_Periods");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(e => e.PeriodType).HasColumnName("period_type").HasMaxLength(10).IsRequired();
        builder.Property(e => e.PeriodDate).HasColumnName("period_date").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(e => e.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.TransactionCount).HasColumnName("transaction_count").IsRequired();
        builder.Property(e => e.PreparedByUserId).HasColumnName("prepared_by_user_id");
        builder.Property(e => e.PreparedAt).HasColumnName("prepared_at");
        builder.Property(e => e.ConfirmedByUserId).HasColumnName("confirmed_by_user_id");
        builder.Property(e => e.ConfirmedAt).HasColumnName("confirmed_at");
        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();

        builder.HasIndex(e => new { e.CompanyId, e.PeriodType, e.PeriodDate })
            .IsUnique()
            .HasDatabaseName("UQ_RP_company_period_type_date");
        builder.HasIndex(e => e.Status).HasDatabaseName("IX_RP_status");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .HasConstraintName("FK_RP_company_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.PreparedByUserId)
            .HasConstraintName("FK_RP_prepared_by_user_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.ConfirmedByUserId)
            .HasConstraintName("FK_RP_confirmed_by_user_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
