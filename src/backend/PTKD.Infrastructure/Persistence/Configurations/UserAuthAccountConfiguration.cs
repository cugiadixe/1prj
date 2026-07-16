using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public sealed class UserAuthAccountConfiguration : IEntityTypeConfiguration<UserAuthAccount>
{
    public void Configure(EntityTypeBuilder<UserAuthAccount> builder)
    {
        builder.ToTable("User_Auth_Accounts", "dbo");

        builder.HasKey(account => account.Id)
            .HasName("PK_User_Auth_Accounts");

        builder.Property(account => account.Id)
            .HasColumnName("id");
        builder.Property(account => account.UserId)
            .HasColumnName("user_id")
            .IsRequired();
        builder.Property(account => account.ProviderType)
            .HasColumnName("provider_type")
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(account => account.ProviderSubject)
            .HasColumnName("provider_subject")
            .HasMaxLength(200)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(account => account.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsUnicode(false);
        builder.Property(account => account.AuthAccountStatus)
            .HasColumnName("auth_account_status")
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(account => account.FailedAttemptCount)
            .HasColumnName("failed_attempt_count")
            .IsRequired();
        builder.Property(account => account.LockoutEnd)
            .HasColumnName("lockout_end")
            .HasColumnType("datetime2(3)");
        builder.Property(account => account.MustChangePassword)
            .HasColumnName("must_change_password")
            .IsRequired();
        builder.Property(account => account.TemporaryPasswordExpiresAt)
            .HasColumnName("temporary_password_expires_at")
            .HasColumnType("datetime2(3)");
        builder.Property(account => account.SecurityStamp)
            .HasColumnName("security_stamp")
            .IsRequired();
        builder.Property(account => account.SessionsInvalidatedAt)
            .HasColumnName("sessions_invalidated_at")
            .HasColumnType("datetime2(3)");
        builder.Property(account => account.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2(3)")
            .IsRequired();
        builder.Property(account => account.CreatedByUserId)
            .HasColumnName("created_by_user_id");
        builder.Property(account => account.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2(3)");
        builder.Property(account => account.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");
        builder.Property(account => account.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion()
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(account => new { account.ProviderType, account.ProviderSubject })
            .IsUnique()
            .HasDatabaseName("UQ_UserAuthAccounts_ProviderSubject");
        builder.HasIndex(account => account.UserId)
            .HasDatabaseName("IX_UserAuthAccounts_UserId");

        builder.HasOne(account => account.User)
            .WithMany()
            .HasForeignKey(account => account.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserAuthAccounts_User");
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(account => account.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserAuthAccounts_CreatedBy");
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(account => account.UpdatedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserAuthAccounts_UpdatedBy");
    }
}
