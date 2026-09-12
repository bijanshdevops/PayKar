using IndustrialPlatform.Domain.Ads;

namespace IndustrialPlatform.Application.Ads;

/// <summary>جایگاه تبلیغاتی از کاتالوگ ثابت — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۳).</summary>
public sealed record BannerSlotDto(
    int Id,
    string Title,
    string Placement,
    string Dimensions,
    decimal DailyPrice,
    bool IsActive);

internal static class BannerSlotMapper
{
    public static BannerSlotDto ToDto(BannerSlot slot) => new(
        slot.Id,
        slot.Title,
        slot.Placement.ToString(),
        slot.Dimensions,
        slot.DailyPrice,
        slot.IsActive);
}
