using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence.Configurations;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards", "dbo");
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.CompanyId).HasColumnName("company_id");
        builder.Property(c => c.GraveId).HasColumnName("grave_id");
        builder.Property(c => c.CardNumber).HasColumnName("card_number").HasMaxLength(50);
        builder.Property(c => c.ServiceId).HasColumnName("service_id");
        builder.Property(c => c.PrintCount).HasColumnName("print_count");
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(50);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        builder.Property(c => c.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(c => c.RowVersion).HasColumnName("row_version").IsRowVersion();
    }
}
