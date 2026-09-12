using IndustrialPlatform.Domain.Ads;

namespace IndustrialPlatform.Application.Ads;

/// <summary>پورت خروجی کاتالوگ جایگاه‌های تبلیغاتی — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۳).</summary>
public interface IBannerSlotRepository
{
    Task<BannerSlot?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BannerSlot>> GetActiveAsync(CancellationToken cancellationToken = default);
}
