using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Dashboard;
using IndustrialPlatform.Domain.Ads;
using IndustrialPlatform.Domain.JobAds;
using IndustrialPlatform.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

/// <summary>پیاده‌سازی Read-Only آمار داشبورد پنل شرکت — طبق تسک #74 و فاز بازطراحی پیکسل‌به‌پیکسل (عیناً الگوی OwnerDashboardQueryService).</summary>
public sealed class CompanyDashboardQueryService : ICompanyDashboardQueryService
{
    private const int GrowthWindowDays = 30;
    private const int SparklineDays = 14;
    private const int ViewsTrendDays = 30;

    private readonly AppDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CompanyDashboardQueryService(AppDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<CompanyDashboardDto> GetDashboardAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var companyJobAds = _dbContext.JobAds.Where(j => j.CompanyId == companyId);

        var jobAdStats = new CompanyJobAdStatsDto(
            TotalJobAds: await companyJobAds.CountAsync(cancellationToken),
            PublishedJobAds: await companyJobAds.CountAsync(j => j.Status == JobAdStatus.Published, cancellationToken),
            PendingReviewJobAds: await companyJobAds.CountAsync(j => j.Status == JobAdStatus.PendingReview, cancellationToken),
            RejectedJobAds: await companyJobAds.CountAsync(j => j.Status == JobAdStatus.Rejected, cancellationToken),
            ExpiredOrClosedJobAds: await companyJobAds.CountAsync(
                j => j.Status == JobAdStatus.Expired || j.Status == JobAdStatus.Closed || j.Status == JobAdStatus.Archived,
                cancellationToken));

        var applicationsPerJobAd = await (
            from jobAd in companyJobAds
            join application in _dbContext.JobApplications on jobAd.Id equals application.JobAdId into apps
            select new JobAdApplicationCountDto(jobAd.Id, jobAd.Title, apps.Count()))
            .OrderByDescending(x => x.ApplicationCount)
            .ToListAsync(cancellationToken);

        var companyBannerAds = _dbContext.BannerAds.Where(b => b.CompanyId == companyId);

        var bannerAdStats = new CompanyBannerAdStatsDto(
            TotalBannerAds: await companyBannerAds.CountAsync(cancellationToken),
            ActiveBannerAds: await companyBannerAds.CountAsync(b => b.Status == BannerAdStatus.Active, cancellationToken),
            PendingReviewBannerAds: await companyBannerAds.CountAsync(b => b.Status == BannerAdStatus.PendingReview, cancellationToken));

        var successfulTransactions = _dbContext.Set<PaymentTransaction>()
            .Where(t => t.CompanyId == companyId && t.Status == PaymentTransactionStatus.Success);

        var paymentSummary = new CompanyPaymentSummaryDto(
            TotalPaidInRials: await successfulTransactions.SumAsync(t => (long?)t.AmountInRials, cancellationToken) ?? 0L,
            TotalSuccessfulTransactions: await successfulTransactions.CountAsync(cancellationToken));

        var companyJobAdIds = companyJobAds.Select(j => j.Id);
        var companyApplications = _dbContext.JobApplications.Where(a => companyJobAdIds.Contains(a.JobAdId));

        var activeJobAdsKpi = await BuildKpiFromTimestampsAsync(
            currentValue: jobAdStats.PublishedJobAds,
            timestampsQuery: companyJobAds.Select(j => j.CreatedAtUtc),
            utcNow,
            cancellationToken);

        var newApplicantsKpi = await BuildKpiFromTimestampsAsync(
            currentValue: await companyApplications.CountAsync(
                a => a.CreatedAtUtc >= utcNow.Date.AddDays(-(GrowthWindowDays - 1)), cancellationToken),
            timestampsQuery: companyApplications.Select(a => a.CreatedAtUtc),
            utcNow,
            cancellationToken);

        var viewsKpi = await BuildViewsKpiAsync(companyId, companyJobAds, utcNow, cancellationToken);

        var viewsByJobAd = await companyJobAds
            .Where(j => j.ViewsCount > 0)
            .OrderByDescending(j => j.ViewsCount)
            .Select(j => new ViewsByJobAdSliceDto(j.Id, j.Title, j.ViewsCount))
            .ToListAsync(cancellationToken);

        var viewsTrend30Days = await BuildViewsTrendAsync(companyId, utcNow, cancellationToken);

        return new CompanyDashboardDto(
            jobAdStats,
            applicationsPerJobAd,
            bannerAdStats,
            paymentSummary,
            activeJobAdsKpi,
            viewsKpi,
            newApplicantsKpi,
            viewsByJobAd,
            viewsTrend30Days);
    }

    /// <summary>محاسبه درصد رشد (۳۰ روز اخیر در مقابل ۳۰ روز پیش از آن) و ریزنمودار ۱۴ روزه بر اساس زمان ایجاد رکوردها.</summary>
    private static async Task<KpiTrendDto> BuildKpiFromTimestampsAsync(
        int currentValue,
        IQueryable<DateTime> timestampsQuery,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var windowStart = utcNow.Date.AddDays(-(GrowthWindowDays - 1));
        var previousWindowStart = windowStart.AddDays(-GrowthWindowDays);

        var currentWindowCount = await timestampsQuery.CountAsync(t => t >= windowStart, cancellationToken);
        var previousWindowCount = await timestampsQuery.CountAsync(t => t >= previousWindowStart && t < windowStart, cancellationToken);
        var growthPercent = ComputeGrowthPercent(currentWindowCount, previousWindowCount);

        var sparklineStart = utcNow.Date.AddDays(-(SparklineDays - 1));
        var rawDaily = await timestampsQuery
            .Where(t => t >= sparklineStart)
            .GroupBy(t => t.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var dailyByDate = rawDaily.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        var sparkline = BuildZeroFilledSparkline(DateOnly.FromDateTime(sparklineStart), SparklineDays, dailyByDate);

        return new KpiTrendDto(currentValue, growthPercent, sparkline);
    }

    /// <summary>
    /// KPI «مجموع بازدیدها» — مقدار فعلی از مجموع لحظه‌ای JobAd.ViewsCount (شمارشگر عمری واقعی)
    /// و رشد/ریزنمودار از جدول CompanyDailyViewStat (که از این فاز به بعد به‌صورت رو-به-جلو تجمیع می‌شود).
    /// </summary>
    private async Task<KpiTrendDto> BuildViewsKpiAsync(
        Guid companyId,
        IQueryable<JobAd> companyJobAds,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var totalViews = await companyJobAds.SumAsync(j => (long?)j.ViewsCount, cancellationToken) ?? 0L;

        var dailyStats = _dbContext.CompanyDailyViewStats.Where(s => s.CompanyId == companyId);

        var windowStart = DateOnly.FromDateTime(utcNow.Date.AddDays(-(GrowthWindowDays - 1)));
        var previousWindowStart = windowStart.AddDays(-GrowthWindowDays);

        var currentWindowViews = await dailyStats
            .Where(s => s.StatDateUtc >= windowStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .SumAsync(s => (int?)s.ViewsCount, cancellationToken) ?? 0;
        var previousWindowViews = await dailyStats
            .Where(s => s.StatDateUtc >= previousWindowStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                     && s.StatDateUtc < windowStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .SumAsync(s => (int?)s.ViewsCount, cancellationToken) ?? 0;
        var growthPercent = ComputeGrowthPercent(currentWindowViews, previousWindowViews);

        var sparklineStart = DateOnly.FromDateTime(utcNow.Date.AddDays(-(SparklineDays - 1)));
        var rawDaily = await dailyStats
            .Where(s => s.StatDateUtc >= sparklineStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .Select(s => new { s.StatDateUtc, s.ViewsCount })
            .ToListAsync(cancellationToken);
        var dailyByDate = rawDaily.ToDictionary(x => DateOnly.FromDateTime(x.StatDateUtc), x => x.ViewsCount);
        var sparkline = BuildZeroFilledSparkline(sparklineStart, SparklineDays, dailyByDate);

        return new KpiTrendDto((int)Math.Min(totalViews, int.MaxValue), growthPercent, sparkline);
    }

    private async Task<IReadOnlyList<DailyViewsPointDto>> BuildViewsTrendAsync(Guid companyId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var startDate = DateOnly.FromDateTime(utcNow.Date).AddDays(-(ViewsTrendDays - 1));
        var startDateTimeUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var rawDaily = await _dbContext.CompanyDailyViewStats
            .Where(s => s.CompanyId == companyId && s.StatDateUtc >= startDateTimeUtc)
            .Select(s => new { s.StatDateUtc, s.ViewsCount })
            .ToListAsync(cancellationToken);

        var dailyByDate = rawDaily.ToDictionary(x => DateOnly.FromDateTime(x.StatDateUtc), x => x.ViewsCount);

        var trend = new List<DailyViewsPointDto>(ViewsTrendDays);
        for (var i = 0; i < ViewsTrendDays; i++)
        {
            var date = startDate.AddDays(i);
            trend.Add(new DailyViewsPointDto(date, dailyByDate.GetValueOrDefault(date, 0)));
        }
        return trend;
    }

    private static IReadOnlyList<int> BuildZeroFilledSparkline(DateOnly startDate, int dayCount, IReadOnlyDictionary<DateOnly, int> dailyByDate)
    {
        var sparkline = new List<int>(dayCount);
        for (var i = 0; i < dayCount; i++)
            sparkline.Add(dailyByDate.GetValueOrDefault(startDate.AddDays(i), 0));
        return sparkline;
    }

    private static decimal ComputeGrowthPercent(long currentValue, long previousValue)
    {
        if (previousValue == 0)
            return currentValue > 0 ? 100m : 0m;
        return Math.Round((decimal)(currentValue - previousValue) / previousValue * 100m, 1);
    }
}
