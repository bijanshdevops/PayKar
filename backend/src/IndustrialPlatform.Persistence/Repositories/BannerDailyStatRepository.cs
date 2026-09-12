using IndustrialPlatform.Application.Ads;
using IndustrialPlatform.Domain.Ads;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

/// <summary>
/// پیاده‌سازی واقعی آمار روزانه بنر با افزایش اتمیک شمارنده‌ها — طبق فاز «مدیریت و رزرو بنرهای
/// تبلیغاتی» (تسک ۶). یادداشت معماری آگاهانه: این ریپازیتوری برخلاف الگوی معمول «Add + یک
/// SaveChanges در پایان Handler توسط IUnitOfWork»، تغییرات را بلافاصله و مستقیماً با
/// ExecuteUpdateAsync/SaveChangesAsync اعمال می‌کند — چون این مسیر پرترافیک‌ترین اندپوینت‌های
/// سیستم (پیکسل بازدید/کلیک عمومی) را پوشش می‌دهد و باید بدون بارگذاری کامل موجودیت و بدون
/// درگیر شدن با توکن همروندی خوش‌بینانه (xmin)، مستقیماً در سطح دیتابیس افزایش اتمیک انجام شود.
/// </summary>
public sealed class BannerDailyStatRepository : IBannerDailyStatRepository
{
    private readonly AppDbContext _dbContext;

    public BannerDailyStatRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task IncrementImpressionAsync(Guid bannerAdId, DateTime utcDate, CancellationToken cancellationToken = default) =>
        UpsertAsync(bannerAdId, utcDate, incrementImpressions: true, cancellationToken);

    public Task IncrementClickAsync(Guid bannerAdId, DateTime utcDate, CancellationToken cancellationToken = default) =>
        UpsertAsync(bannerAdId, utcDate, incrementImpressions: false, cancellationToken);

    private async Task UpsertAsync(Guid bannerAdId, DateTime utcDate, bool incrementImpressions, CancellationToken cancellationToken)
    {
        var date = utcDate.Date;

        var updatedRows = await ExecuteIncrementAsync(bannerAdId, date, incrementImpressions, cancellationToken);
        if (updatedRows > 0)
            return;

        // رکورد امروز هنوز وجود ندارد — تلاش برای ایجاد آن با مقدار اولیه ۱.
        try
        {
            var stat = incrementImpressions
                ? BannerDailyStat.Create(bannerAdId, date, impressionsCount: 1, clicksCount: 0)
                : BannerDailyStat.Create(bannerAdId, date, impressionsCount: 0, clicksCount: 1);

            _dbContext.BannerDailyStats.Add(stat);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // درخواست هم‌زمان دیگری بین بررسی و درج، رکورد امروز را ایجاد کرد (تداخل Unique Index) —
            // اکنون رکورد قطعاً وجود دارد، پس به‌جای درج، افزایش می‌دهیم.
            _dbContext.ChangeTracker.Clear();
            await ExecuteIncrementAsync(bannerAdId, date, incrementImpressions, cancellationToken);
        }
    }

    private Task<int> ExecuteIncrementAsync(Guid bannerAdId, DateTime date, bool incrementImpressions, CancellationToken cancellationToken)
    {
        var query = _dbContext.BannerDailyStats.Where(s => s.BannerAdId == bannerAdId && s.Date == date);

        return incrementImpressions
            ? query.ExecuteUpdateAsync(setters => setters.SetProperty(s => s.ImpressionsCount, s => s.ImpressionsCount + 1), cancellationToken)
            : query.ExecuteUpdateAsync(setters => setters.SetProperty(s => s.ClicksCount, s => s.ClicksCount + 1), cancellationToken);
    }
}
