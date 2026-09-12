using IndustrialPlatform.Domain.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> builder)
    {
        builder.ToTable("support_messages");
        builder.HasKey(m => m.Id).HasName("pk_support_messages");

        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.TicketId).HasColumnName("ticket_id").IsRequired();
        builder.Property(m => m.SenderUserId).HasColumnName("sender_user_id").IsRequired();
        builder.Property(m => m.IsFromSupportTeam).HasColumnName("is_from_support_team").IsRequired();
        builder.Property(m => m.Body).HasColumnName("body").HasColumnType("text").IsRequired();
        // ضمیمه فایل/تصویر اختیاری — طبق فاز «مدیریت پشتیبانی و تیکت‌ها».
        builder.Property(m => m.AttachmentUrl).HasColumnName("attachment_url").HasMaxLength(500);
        builder.Property(m => m.AttachmentFileName).HasColumnName("attachment_file_name").HasMaxLength(255);

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(m => m.TicketId).HasDatabaseName("ix_support_messages_ticket_id");
    }
}
