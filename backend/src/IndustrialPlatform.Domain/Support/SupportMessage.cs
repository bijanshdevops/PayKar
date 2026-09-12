using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Support;

/// <summary>
/// یک پیام درون تیکت پشتیبانی — Aggregate جداگانه از SupportTicket نگه داشته شده
/// چون چرخه حیات مستقلی دارد (مشابه الگوی CompanyDocument نسبت به Company) — طبق ADR-007.
/// </summary>
public sealed class SupportMessage : BaseEntity<Guid>
{
    public Guid TicketId { get; private set; }
    public Guid SenderUserId { get; private set; }

    /// <summary>true یعنی فرستنده عضو تیم پشتیبانی (Admin) است — برای نمایش صحیح در UI بدون Join به Identity.</summary>
    public bool IsFromSupportTeam { get; private set; }

    public string Body { get; private set; } = string.Empty;

    /// <summary>مسیر URL نسبی ضمیمه (تصویر/PDF/سند) — اختیاری، طبق فاز «مدیریت پشتیبانی و تیکت‌ها».</summary>
    public string? AttachmentUrl { get; private set; }

    /// <summary>نام اصلی فایل ضمیمه (برای نمایش/دانلود با نام خوانا) — اختیاری.</summary>
    public string? AttachmentFileName { get; private set; }

    private SupportMessage() { }

    private SupportMessage(
        Guid id, Guid ticketId, Guid senderUserId, bool isFromSupportTeam, string body,
        string? attachmentUrl, string? attachmentFileName) : base(id)
    {
        TicketId = ticketId;
        SenderUserId = senderUserId;
        IsFromSupportTeam = isFromSupportTeam;
        Body = body;
        AttachmentUrl = attachmentUrl;
        AttachmentFileName = attachmentFileName;
    }

    public static SupportMessage Create(
        Guid ticketId, Guid senderUserId, bool isFromSupportTeam, string body,
        string? attachmentUrl = null, string? attachmentFileName = null) =>
        new(Guid.NewGuid(), ticketId, senderUserId, isFromSupportTeam, body, attachmentUrl, attachmentFileName);
}
