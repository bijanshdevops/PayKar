namespace IndustrialPlatform.Application.Dashboard;

/// <summary>آمار کلی پلتفرم — طبق ADR-009.</summary>
public sealed record PlatformOverviewDto(
    int TotalCompanies,
    int VerifiedCompanies,
    int TotalJobAds,
    int PublishedJobAds,
    int PendingReviewJobAds,
    int TotalCandidates,
    int TotalApplications,
    int ActiveBannerAds,
    int OpenSupportTickets);

/// <summary>آمار مالی/درآمد — طبق ADR-009.</summary>
public sealed record RevenueSummaryDto(
    long TotalRevenueInRials,
    long JobAdFeeRevenueInRials,
    long BannerAdFeeRevenueInRials,
    int TotalSuccessfulTransactions);

/// <summary>یک نقطه از روند زمانی درآمد روزانه — طبق ADR-009.</summary>
public sealed record DailyRevenuePointDto(DateOnly Date, long AmountInRials);

/// <summary>خروجی کامل داشبورد Owner — طبق ADR-009.</summary>
public sealed record OwnerDashboardDto(
    PlatformOverviewDto Overview,
    RevenueSummaryDto Revenue,
    IReadOnlyList<DailyRevenuePointDto> RevenueTimeSeries);
