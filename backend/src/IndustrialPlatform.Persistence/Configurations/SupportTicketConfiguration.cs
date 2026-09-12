using IndustrialPlatform.Domain.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("support_tickets");
        builder.HasKey(t => t.Id).HasName("pk_support_tickets");

        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();
        // شماره موبایل کاربر ثبت‌کننده در لحظه ایجاد تیکت — Denormalized، طبق فاز «مدیریت پشتیبانی و تیکت‌ها».
        builder.Property(t => t.RequesterMobileNumber).HasColumnName("requester_mobile_number").HasMaxLength(20).IsRequired();
        // طبق فاز «مدیریت پشتیبانی و تیکت‌ها» — شناسه خوانا (مثلاً TK-2026-0001)، یکتا در کل جدول.
        builder.Property(t => t.TicketNumber).HasColumnName("ticket_number").HasMaxLength(30).IsRequired();
        builder.Property(t => t.Subject).HasColumnName("subject").HasMaxLength(200).IsRequired();
        builder.Property(t => t.Department).HasColumnName("department").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.Priority).HasColumnName("priority").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(t => t.UserId).HasDatabaseName("ix_support_tickets_user_id");
        builder.HasIndex(t => t.Status).HasDatabaseName("ix_support_tickets_status");
        builder.HasIndex(t => t.TicketNumber).HasDatabaseName("ux_support_tickets_ticket_number").IsUnique();
    }
}
