using IndustrialPlatform.Application.Dashboard;
using IndustrialPlatform.Domain.Ads;
using IndustrialPlatform.Domain.Companies;
using IndustrialPlatform.Domain.JobAds;
using IndustrialPlatform.Domain.Payments;
using IndustrialPlatform.Domain.Support;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

/// <summary>پیاده‌سازی Read-Only آمار داشبورد پنل Owner — طبق ADR-009 (عیناً الگوی GeographyQueryService).</summary>
public sealed class OwnerDashboardQueryService : IOwnerDashboardQueryService
{
    private readonly AppDbContext _dbContext;

    public OwnerDashboardQueryService(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<OwnerDashboardDto> GetDashboardAsync(DateTime utcNow, int timeSeriesDays, CancellationToken cancellationToken = default)
    {
        var overview = new PlatformOverviewDto(
            TotalCompanies: await _dbContext.Companies.CountAsync(cancellationToken),
            VerifiedCompanies: await _dbContext.Companies.CountAsync(c => c.VerificationStatus == VerificationStatus.Verified, cancellationToken),
            TotalJobAds: await _dbContext.JobAds.CountAsync(cancellationToken),
            PublishedJobAds: await _dbContext.JobAds.CountAsync(j => j.Status == JobAdStatus.Published, cancellationToken),
            PendingReviewJobAds: await _dbContext.JobAds.CountAsync(j => j.Status == JobAdStatus.PendingReview, cancellationToken),
            TotalCandidates: await _dbContext.Candidates.CountAsync(cancellationToken),
            TotalApplications: await _dbContext.JobApplications.CountAsync(cancellationToken),
            ActiveBannerAds: await _dbContext.BannerAds.CountAsync(b => b.Status == BannerAdStatus.Active, cancellationToken),
            OpenSupportTickets: await _dbContext.SupportTickets.CountAsync(
                t => t.Status == SupportTicketStatus.PendingResponse || t.Status == SupportTicketStatus.InProgress || t.Status == SupportTicketStatus.Reopened,
                cancellationToken));

        var successfulTransactions = _dbContext.Set<PaymentTransaction>().Where(t => t.Status == PaymentTransactionStatus.Success);

        var jobAdRevenue = await successfulTransactions
            .Where(t => t.Purpose == PaymentPurpose.JobAdListingFee)
            .SumAsync(t => (long?)t.AmountInRials, cancellationToken) ?? 0L;

        var bannerAdRevenue = await successfulTransactions
            .Where(t => t.Purpose == PaymentPurpose.BannerAdFee)
            .SumAsync(t => (long?)t.AmountInRials, cancellationToken) ?? 0L;

        var totalSuccessfulTransactions = await successfulTransactions.CountAsync(cancellationToken);

        var revenue = new RevenueSummaryDto(
            TotalRevenueInRials: jobAdRevenue + bannerAdRevenue,
            JobAdFeeRevenueInRials: jobAdRevenue,
            BannerAdFeeRevenueInRials: bannerAdRevenue,
            TotalSuccessfulTransactions: totalSuccessfulTransactions);

        var startDate = DateOnly.FromDateTime(utcNow.Date).AddDays(-(timeSeriesDays - 1));
        var startDateTimeUtc = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var rawDailyRevenue = await successfulTransactions
            .Where(t => t.CreatedAtUtc >= startDateTimeUtc)
            .GroupBy(t => t.CreatedAtUtc.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(t => t.AmountInRials) })
            .ToListAsync(cancellationToken);

        var dailyRevenueByDate = rawDailyRevenue.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Amount);

        var timeSeries = new List<DailyRevenuePointDto>(timeSeriesDays);
        for (var i = 0; i < timeSeriesDays; i++)
        {
            var date = startDate.AddDays(i);
            timeSeries.Add(new DailyRevenuePointDto(date, dailyRevenueByDate.GetValueOrDefault(date, 0L)));
        }

        return new OwnerDashboardDto(overview, revenue, timeSeries);
    }
}
