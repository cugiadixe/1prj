using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Security.Authentication;

namespace PTKD.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("Refresh_Tokens", "dbo");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        
        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .IsRequired()
            .HasColumnType("char(64)");

        builder.Property(x => x.FamilyId).HasColumnName("family_id");
        builder.Property(x => x.SessionId).HasColumnName("session_id");
        builder.Property(x => x.IssuedAt).HasColumnName("issued_at");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.UsedAt).HasColumnName("used_at");
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        builder.Property(x => x.RevokeReason).HasColumnName("revoke_reason");
        builder.Property(x => x.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        builder.Property(x => x.ReuseDetectedAt).HasColumnName("reuse_detected_at");
        builder.Property(x => x.CreatedIpAddress).HasColumnName("created_ip_address");
        builder.Property(x => x.CreatedUserAgent).HasColumnName("created_user_agent");

        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion()
            .IsRequired();

        // One-to-many relationship with UserAuthAccount
        builder.HasOne<PTKD.Domain.Entities.UserAuthAccount>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing relationship for replacement token
        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(x => x.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
