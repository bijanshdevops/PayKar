namespace IndustrialPlatform.Application.Support;

/// <summary>
/// تولید شناسه خوانای تیکت به‌فرمت «TK-{سال میلادی}-{شماره ترتیبی ۴رقمی}» (مثلاً TK-2026-0001) —
/// طبق فاز «مدیریت پشتیبانی و تیکت‌ها». شماره ترتیبی بر اساس شمارش تیکت‌های همان سال محاسبه می‌شود؛
/// برای مقیاس فعلی این پلتفرم (بدون نیاز به تضمین همزمانی سخت‌گیرانه) کافی است.
/// </summary>
internal static class TicketNumberGenerator
{
    public static string Generate(int year, int existingCountForYear) =>
        $"TK-{year}-{(existingCountForYear + 1).ToString("D4")}";
}
