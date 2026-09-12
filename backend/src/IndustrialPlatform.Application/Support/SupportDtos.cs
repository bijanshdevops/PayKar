namespace IndustrialPlatform.Application.Support;

public sealed record SupportMessageDto(
    Guid Id,
    Guid SenderUserId,
    bool IsFromSupportTeam,
    string Body,
    // ضمیمه فایل/تصویر اختیاری — طبق فاز «مدیریت پشتیبانی و تیکت‌ها».
    string? AttachmentUrl,
    string? AttachmentFileName,
    DateTime CreatedAtUtc);

public sealed record SupportTicketDto(
    Guid Id,
    string TicketNumber,
    Guid UserId,
    string Subject,
    string Department,
    string Priority,
    string Status,
    DateTime CreatedAtUtc,
    // آخرین فعالیت واقعی روی تیکت (حداکثرِ CreatedAtUtc/LastModifiedAtUtc خودِ تیکت و جدیدترین پیام) —
    // برای ستون «آخرین به‌روزرسانی» صفحه لیست، طبق فاز «مدیریت پشتیبانی و تیکت‌ها».
    DateTime LastActivityAtUtc);

public sealed record SupportTicketDetailDto(
    Guid Id,
    string TicketNumber,
    Guid UserId,
    string Subject,
    string Department,
    string Priority,
    string Status,
    DateTime CreatedAtUtc,
    DateTime LastActivityAtUtc,
    IReadOnlyList<SupportMessageDto> Messages,
    // شماره موبایل کاربر ثبت‌کننده — برای کارت «کاربر مرتبط» صفحه جزئیات تیکت، طبق فاز «مدیریت پشتیبانی و تیکت‌ها».
    string RequesterMobileNumber);

/// <summary>شمارنده تیکت‌ها به تفکیک وضعیت — برای تب‌های فیلتر صفحه لیست، طبق فاز «مدیریت پشتیبانی و تیکت‌ها».</summary>
public sealed record SupportTicketStatusSummaryDto(
    int All,
    int PendingResponse,
    int InProgress,
    int Answered,
    int Closed,
    int Reopened);
