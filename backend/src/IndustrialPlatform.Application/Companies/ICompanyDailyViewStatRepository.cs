using IndustrialPlatform.Domain.Companies;

namespace IndustrialPlatform.Application.Companies;

/// <summary>
/// پورت خروجی آمار بازدید روزانه شرکت — طبق فاز بازطراحی داشبورد کارفرما، برای امکان محاسبه واقعی
/// روند/رشد بازدید (اسپارک‌لاین KPI و نمودار ناحیه‌ای ۳۰ روزه) بدون داده جعلی.
/// پیاده‌سازی واقعی در IndustrialPlatform.Persistence.
/// </summary>
public interface ICompanyDailyViewStatRepository
{
    Task<CompanyDailyViewStat?> GetByCompanyAndDateAsync(Guid companyId, DateTime statDateUtc, CancellationToken cancellationToken = default);
    void Add(CompanyDailyViewStat stat);
}
