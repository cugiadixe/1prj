using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CustomerMergeRequestCandidateConfiguration : IEntityTypeConfiguration<CustomerMergeRequestCandidate>
{
    public void Configure(EntityTypeBuilder<CustomerMergeRequestCandidate> builder)
    {
        builder.ToTable("Customer_Merge_Request_Candidates");

        builder.HasKey(e => e.Id).HasName("PK_Customer_Merge_Request_Candidates");
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.MergeRequestId).HasColumnName("merge_request_id").IsRequired();
        builder.Property(e => e.CandidateCustomerId).HasColumnName("candidate_customer_id").IsRequired();
        builder.Property(e => e.MatchType).HasColumnName("match_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.MatchConfidence).HasColumnName("match_confidence").HasColumnType("decimal(5,2)");
        builder.Property(e => e.SnapshotPayload).HasColumnName("snapshot_payload");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(e => e.MergeRequestId).HasDatabaseName("IX_CMRC_merge_request");

        builder.HasOne(e => e.CandidateCustomer)
            .WithMany()
            .HasForeignKey(e => e.CandidateCustomerId)
            .HasConstraintName("FK_CMRC_candidate_customer_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
