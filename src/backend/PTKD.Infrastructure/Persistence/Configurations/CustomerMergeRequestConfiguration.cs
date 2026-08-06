using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CustomerMergeRequestConfiguration : IEntityTypeConfiguration<CustomerMergeRequest>
{
    public void Configure(EntityTypeBuilder<CustomerMergeRequest> builder)
    {
        builder.ToTable("Customer_Merge_Requests");

        builder.HasKey(e => e.Id).HasName("PK_Customer_Merge_Requests");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.SourceCustomerId).HasColumnName("source_customer_id").IsRequired();
        builder.Property(e => e.TargetCustomerId).HasColumnName("target_customer_id").IsRequired();
        builder.Property(e => e.RequesterId).HasColumnName("requester_id").IsRequired();
        builder.Property(e => e.RequestStatus).HasColumnName("request_status").HasMaxLength(30).IsRequired();
        builder.Property(e => e.SurvivorshipPayload).HasColumnName("survivorship_payload").IsRequired();
        builder.Property(e => e.SourceRowVersionSnapshot).HasColumnName("source_rowversion_snapshot").IsRequired();
        builder.Property(e => e.TargetRowVersionSnapshot).HasColumnName("target_rowversion_snapshot").IsRequired();
        builder.Property(e => e.WorkflowInstanceId).HasColumnName("workflow_instance_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();

        builder.HasIndex(e => e.SourceCustomerId).HasDatabaseName("IX_CMR_source_customer");
        builder.HasIndex(e => e.TargetCustomerId).HasDatabaseName("IX_CMR_target_customer");

        builder.HasOne(e => e.SourceCustomer)
            .WithMany()
            .HasForeignKey(e => e.SourceCustomerId)
            .HasConstraintName("FK_CMR_source_customer_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TargetCustomer)
            .WithMany()
            .HasForeignKey(e => e.TargetCustomerId)
            .HasConstraintName("FK_CMR_target_customer_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Candidates)
            .WithOne(e => e.MergeRequest)
            .HasForeignKey(e => e.MergeRequestId)
            .HasConstraintName("FK_CMRC_merge_request_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
