namespace IndustrialPlatform.Application.Dashboard;

/// <summary>پورت Read-Only آمار داشبورد پنل شرکت — طبق تسک #74 (عیناً الگوی IOwnerDashboardQueryService/ADR-009).</summary>
public interface ICompanyDashboardQueryService
{
    Task<CompanyDashboardDto> GetDashboardAsync(Guid companyId, CancellationToken cancellationToken = default);
}
