using IndustrialPlatform.Domain.Ads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Persistence.Configurations;

/// <summary>
/// کاتالوگ ثابت جایگاه‌های تبلیغاتی — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۱).
/// Seed Data شامل ۳ جایگاه واقعی تصمیم‌گیری‌شده محصولی (قیمت/ابعاد دقیق طبق مشخصات این فاز)،
/// از طریق HasData تا مستقیماً در مایگریشن اولیه به‌صورت INSERT درج شود.
/// </summary>
public sealed class BannerSlotConfiguration : IEntityTypeConfiguration<BannerSlot>
{
    /// <summary>تاریخ ثابت ایجاد رکوردهای کاتالوگ (Seed Data) — طبق الزام HasData به مقادیر ایستا برای ستون‌های Required.</summary>
    private static readonly DateTime SeedCreatedAtUtc = new(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<BannerSlot> builder)
    {
        builder.ToTable("banner_slots");
        builder.HasKey(s => s.Id).HasName("pk_banner_slots");

        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
        builder.Property(s => s.Placement).HasColumnName("placement").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(s => s.Dimensions).HasColumnName("dimensions").HasMaxLength(30).IsRequired();
        builder.Property(s => s.DailyPrice).HasColumnName("daily_price").HasPrecision(18, 2).IsRequired();
        builder.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();

        PersistenceHelpers.ConfigureAuditColumns(builder);

        builder.HasIndex(s => s.Placement).HasDatabaseName("ix_banner_slots_placement");

        // طبق تصمیم قطعی بیزینس این فاز — کاتالوگ ثابت ۳ جایگاه، بدون داده نمونه/فیک.
        builder.HasData(
            new
            {
                Id = 1,
                Title = "جایگاه هدر صفحه اصلی",
                Placement = BannerPlacement.Home,
                Dimensions = "1200x300",
                DailyPrice = 550_000m,
                IsActive = true,
                CreatedAtUtc = SeedCreatedAtUtc
            },
            new
            {
                Id = 2,
                Title = "جایگاه لیست آگهی‌ها",
                Placement = BannerPlacement.JobAdList,
                Dimensions = "728x90",
                DailyPrice = 350_000m,
                IsActive = true,
                CreatedAtUtc = SeedCreatedAtUtc
            },
            new
            {
                Id = 3,
                Title = "جایگاه بنر میانی / فوتر",
                Placement = BannerPlacement.Both,
                Dimensions = "728x90",
                DailyPrice = 150_000m,
                IsActive = true,
                CreatedAtUtc = SeedCreatedAtUtc
            });
    }
}
