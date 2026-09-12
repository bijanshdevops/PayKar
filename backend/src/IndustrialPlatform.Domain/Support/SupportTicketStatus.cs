namespace IndustrialPlatform.Domain.Support;

/// <summary>
/// طبق ADR-007 سند 08-Decision-Log.md، بازنگری‌شده در فاز «مدیریت پشتیبانی و تیکت‌ها» —
/// ۵ وضعیت مجزا (قبلاً ۴ وضعیت بود: Open/InProgress/Resolved/Closed). «بازشده مجدد» اکنون یک
/// وضعیت واقعاً مستقل است (نه صرفاً بازگشت به وضعیت اول)، تا در تب‌های فیلتر و شمارنده‌ها قابل تفکیک باشد.
/// نگاشت داده‌های قدیمی (مایگریشن AddTicketPriorityDepartmentAndRenameStatus): Open→PendingResponse،
/// Resolved→Answered؛ InProgress و Closed بدون تغییر نام باقی مانده‌اند.
/// </summary>
public enum SupportTicketStatus
{
    PendingResponse = 1,
    InProgress = 2,
    Answered = 3,
    Closed = 4,
    Reopened = 5
}
