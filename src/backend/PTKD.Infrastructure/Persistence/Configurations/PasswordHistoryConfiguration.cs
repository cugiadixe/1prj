using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public sealed class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.ToTable("Password_History", "dbo");

        builder.HasKey(history => history.Id)
            .HasName("PK_Password_History");
        builder.Property(history => history.Id)
            .HasColumnName("id");
        builder.Property(history => history.AccountId)
            .HasColumnName("account_id")
            .IsRequired();
        builder.Property(history => history.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(history => history.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.HasIndex(history => new { history.AccountId, history.CreatedAt, history.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("IX_PasswordHistory_Account_CreatedAt");

        builder.HasOne(history => history.Account)
            .WithMany(account => account.PasswordHistories)
            .HasForeignKey(history => history.AccountId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_PasswordHistory_Account");
    }
}
