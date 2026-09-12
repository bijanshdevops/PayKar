namespace IndustrialPlatform.Application.Dashboard;

/// <summary>
/// پورت خروجی خواندن آمار تجمیعی داشبورد پنل Owner (Read-Only) — پیاده‌سازی در Persistence.
/// طبق ADR-009: از الگوی Repository کامل صرف‌نظر شده چون این صرفاً نمای گزارشی چند-Aggregate است
/// (عیناً الگوی IGeographyQueryService).
/// </summary>
public interface IOwnerDashboardQueryService
{
    Task<OwnerDashboardDto> GetDashboardAsync(DateTime utcNow, int timeSeriesDays, CancellationToken cancellationToken = default);
}
