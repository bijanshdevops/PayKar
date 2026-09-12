using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Ads;

/// <summary>
/// آمار روزانه سبک بنر تبلیغاتی (Impression/Click) — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۶).
/// هر رکورد نمایانگر جمع بازدید/کلیک‌های یک بنر در یک روز (UTC) است. از شناسه عددی افزایشی
/// (bigserial) به‌جای Guid استفاده می‌شود چون این جدول درج/به‌روزرسانی پرحجم و پرتکرار دارد.
/// افزایش شمارنده‌ها همیشه از طریق کوئری اتمیک در لایه Persistence انجام می‌شود، نه بارگذاری کامل
/// این موجودیت — بنابراین متد دامنه‌ای برای افزایش این‌جا تعریف نشده است.
/// </summary>
public sealed class BannerDailyStat : BaseEntity<long>
{
    public Guid BannerAdId { get; private set; }
    public BannerAd? BannerAd { get; private set; }

    /// <summary>تاریخ روز (فقط بخش تاریخ، بدون زمان — UTC).</summary>
    public DateTime Date { get; private set; }

    public long ImpressionsCount { get; private set; }
    public long ClicksCount { get; private set; }

    private BannerDailyStat() { }

    private BannerDailyStat(Guid bannerAdId, DateTime date, long impressionsCount, long clicksCount)
    {
        BannerAdId = bannerAdId;
        Date = date.Date;
        ImpressionsCount = impressionsCount;
        ClicksCount = clicksCount;
    }

    public static BannerDailyStat Create(Guid bannerAdId, DateTime date, long impressionsCount = 0, long clicksCount = 0) =>
        new(bannerAdId, date, impressionsCount, clicksCount);
}
