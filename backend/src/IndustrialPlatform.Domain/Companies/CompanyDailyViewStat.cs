using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Companies;

/// <summary>
/// آمار بازدید روزانه یک شرکت — طبق فاز بازطراحی پیکسل‌به‌پیکسل داشبورد کارفرما، برای امکان محاسبه
/// واقعی روند/رشد بازدید (اسپارک‌لاین KPI و نمودار ناحیه‌ای ۳۰ روزه «گزارش بازدید آگهی‌ها») بدون
/// نیاز به فیلد جعلی. هر رکورد نمایانگر جمع بازدیدهای تمام آگهی‌های یک شرکت در یک روز (UTC) است.
/// افزایشی و رو-به-جلو (Forward-accumulating) — بدون Backfill برای بازدیدهای پیش از این تغییر.
/// </summary>
public sealed class CompanyDailyViewStat : AggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }

    /// <summary>تاریخ روز (فقط بخش تاریخ، بدون زمان — UTC).</summary>
    public DateTime StatDateUtc { get; private set; }

    public int ViewsCount { get; private set; }

    private CompanyDailyViewStat() { }

    private CompanyDailyViewStat(Guid id, Guid companyId, DateTime statDateUtc) : base(id)
    {
        CompanyId = companyId;
        StatDateUtc = statDateUtc.Date;
        ViewsCount = 0;
    }

    public static CompanyDailyViewStat Create(Guid companyId, DateTime statDateUtc) =>
        new(Guid.NewGuid(), companyId, statDateUtc);

    public void IncrementViews() => ViewsCount += 1;
}
