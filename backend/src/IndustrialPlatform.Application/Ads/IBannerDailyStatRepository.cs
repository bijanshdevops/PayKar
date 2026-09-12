namespace IndustrialPlatform.Application.Ads;

/// <summary>
/// پورت خروجی آمار روزانه بنر — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۶).
/// پیاده‌سازی واقعی این متدها باید افزایش شمارنده را به‌صورت اتمیک در سطح دیتابیس انجام دهد
/// (نه از طریق بارگذاری/Save کامل موجودیت) تا زیر بار همزمانی بالای پیکسل ردیابی، شمارشی گم نشود.
/// </summary>
public interface IBannerDailyStatRepository
{
    Task IncrementImpressionAsync(Guid bannerAdId, DateTime utcDate, CancellationToken cancellationToken = default);
    Task IncrementClickAsync(Guid bannerAdId, DateTime utcDate, CancellationToken cancellationToken = default);
}
