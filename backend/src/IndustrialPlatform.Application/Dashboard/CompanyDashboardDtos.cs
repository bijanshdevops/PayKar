namespace IndustrialPlatform.Application.Dashboard;

/// <summary>آمار اختصاصی پنل شرکت — طبق تسک #74.</summary>
public sealed record CompanyJobAdStatsDto(
    int TotalJobAds,
    int PublishedJobAds,
    int PendingReviewJobAds,
    int RejectedJobAds,
    int ExpiredOrClosedJobAds);

/// <summary>تعداد رزومه‌های دریافتی به تفکیک هر آگهی — برای نمایش در جدول «رزومه‌های دریافتی».</summary>
public sealed record JobAdApplicationCountDto(Guid JobAdId, string JobAdTitle, int ApplicationCount);

public sealed record CompanyBannerAdStatsDto(int TotalBannerAds, int ActiveBannerAds, int PendingReviewBannerAds);

public sealed record CompanyPaymentSummaryDto(long TotalPaidInRials, int TotalSuccessfulTransactions);

/// <summary>
/// یک شاخص KPI به همراه درصد رشد نسبت به ۳۰ روز پیش از آن و ریزنمودار روزانه (۱۴ روز اخیر) —
/// طبق فاز بازطراحی پیکسل‌به‌پیکسل داشبورد کارفرما (کارت‌های آماری بالای صفحه). تمام مقادیر از
/// داده واقعی بک‌اند محاسبه می‌شوند؛ در نبود داده کافی (مثلاً بازدید در روزهای اخیر پیاده‌سازی این
/// قابلیت) مقدار صفر برگردانده می‌شود نه داده جعلی.
/// </summary>
public sealed record KpiTrendDto(int CurrentValue, decimal GrowthPercent, IReadOnlyList<int> Sparkline);

/// <summary>سهم بازدید هر آگهی از کل بازدیدهای شرکت — برای نمودار دونات «بازدیدها بر اساس آگهی».</summary>
public sealed record ViewsByJobAdSliceDto(Guid JobAdId, string JobAdTitle, int ViewsCount);

/// <summary>یک نقطه از روند بازدید روزانه شرکت — برای نمودار ناحیه‌ای «گزارش بازدید آگهی‌ها».</summary>
public sealed record DailyViewsPointDto(DateOnly Date, int ViewsCount);

public sealed record CompanyDashboardDto(
    CompanyJobAdStatsDto JobAdStats,
    IReadOnlyList<JobAdApplicationCountDto> ApplicationsPerJobAd,
    CompanyBannerAdStatsDto BannerAdStats,
    CompanyPaymentSummaryDto PaymentSummary,
    KpiTrendDto ActiveJobAdsKpi,
    KpiTrendDto ViewsKpi,
    KpiTrendDto NewApplicantsKpi,
    IReadOnlyList<ViewsByJobAdSliceDto> ViewsByJobAd,
    IReadOnlyList<DailyViewsPointDto> ViewsTrend30Days);
