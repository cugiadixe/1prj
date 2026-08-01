using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CustomerMergeHistoryConfiguration : IEntityTypeConfiguration<CustomerMergeHistory>
{
    public void Configure(EntityTypeBuilder<CustomerMergeHistory> builder)
    {
        builder.ToTable("Customer_Merge_History");

        builder.HasKey(e => e.Id).HasName("PK_Customer_Merge_History");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.MergeRequestId).HasColumnName("merge_request_id");
        builder.Property(e => e.SourceCustomerId).HasColumnName("source_customer_id").IsRequired();
        builder.Property(e => e.TargetCustomerId).HasColumnName("target_customer_id").IsRequired();
        builder.Property(e => e.ActionType).HasColumnName("action_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.ActorId).HasColumnName("actor_id").IsRequired();
        builder.Property(e => e.SummaryPayload).HasColumnName("summary_payload").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne(e => e.MergeRequest)
            .WithMany()
            .HasForeignKey(e => e.MergeRequestId)
            .HasConstraintName("FK_CMH_merge_request_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SourceCustomer)
            .WithMany()
            .HasForeignKey(e => e.SourceCustomerId)
            .HasConstraintName("FK_CMH_source_customer_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TargetCustomer)
            .WithMany()
            .HasForeignKey(e => e.TargetCustomerId)
            .HasConstraintName("FK_CMH_target_customer_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
