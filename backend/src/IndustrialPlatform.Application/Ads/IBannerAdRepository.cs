using IndustrialPlatform.Domain.Ads;
using IndustrialPlatform.Shared.Api;

namespace IndustrialPlatform.Application.Ads;

public interface IBannerAdRepository
{
    Task<BannerAd?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<BannerAd>> GetByCompanyIdAsync(Guid companyId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<BannerAd>> GetPendingReviewAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BannerAd>> GetActiveByPlacementAsync(BannerPlacement placement, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BannerAd>> GetExpiringActiveAsync(DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>
    /// بررسی تداخل بازه نمایش — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۴).
    /// اولین بنر دیگر با وضعیت Active روی همان جایگاه که هنوز منقضی نشده (EndDate بزرگ‌تر از اکنون) را برمی‌گرداند.
    /// </summary>
    Task<BannerAd?> GetActiveConflictingBySlotAsync(int bannerSlotId, Guid excludeBannerAdId, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>افزایش اتمیک شمارنده بازدید در دیتابیس — طبق تسک ۶، بدون بارگذاری/ردیابی کامل موجودیت.</summary>
    Task IncrementImpressionCountAsync(Guid bannerAdId, CancellationToken cancellationToken = default);

    /// <summary>افزایش اتمیک شمارنده کلیک در دیتابیس — مشابه بالا.</summary>
    Task IncrementClickCountAsync(Guid bannerAdId, CancellationToken cancellationToken = default);

    void Add(BannerAd bannerAd);
}
