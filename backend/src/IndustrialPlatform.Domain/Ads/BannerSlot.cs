using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Ads;

/// <summary>
/// جایگاه تبلیغاتی از کاتالوگ ثابت پلتفرم — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۱).
/// جایگزین مدل قدیمی «هزینه ثابت واحد برای همه بنرها» با کاتالوگ چند-جایگاهی که هرکدام ابعاد
/// و قیمت روزانه مستقل خود را دارند. یک رکورد مرجع/کاتالوگی است (شبیه Province/City) که با شناسه
/// عددی ثابت (Seed Data) شناسایی می‌شود، نه توسط شرکت‌ها ایجاد می‌شود.
/// </summary>
public sealed class BannerSlot : BaseEntity<int>
{
    public string Title { get; private set; } = string.Empty;
    public BannerPlacement Placement { get; private set; }
    public string Dimensions { get; private set; } = string.Empty;
    public decimal DailyPrice { get; private set; }
    public bool IsActive { get; private set; } = true;

    private BannerSlot() { }

    private BannerSlot(int id, string title, BannerPlacement placement, string dimensions, decimal dailyPrice) : base(id)
    {
        Title = title;
        Placement = placement;
        Dimensions = dimensions;
        DailyPrice = dailyPrice;
        IsActive = true;
    }

    /// <summary>برای ساخت رکوردهای کاتالوگ (Seed Data) با شناسه عددی از پیش تعیین‌شده.</summary>
    public static BannerSlot Create(int id, string title, BannerPlacement placement, string dimensions, decimal dailyPrice) =>
        new(id, title, placement, dimensions, dailyPrice);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
