namespace IndustrialPlatform.Application.Dashboard;

/// <summary>
/// پورت خروجی خواندن آمار عمومی پلتفرم (Read-Only، بدون احراز هویت) — پیاده‌سازی در Persistence.
/// طبق ADR-009: عیناً الگوی IGeographyQueryService/IOwnerDashboardQueryService — نمای گزارشی چند-Aggregate.
/// </summary>
public interface IPublicStatsQueryService
{
    Task<PublicStatsDto> GetPublicStatsAsync(CancellationToken cancellationToken = default);
}
