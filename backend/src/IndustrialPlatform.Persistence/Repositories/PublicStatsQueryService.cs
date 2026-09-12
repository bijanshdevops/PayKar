using IndustrialPlatform.Application.Dashboard;
using IndustrialPlatform.Domain.Companies;
using IndustrialPlatform.Domain.JobAds;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

/// <summary>پیاده‌سازی Read-Only آمار عمومی پلتفرم — عیناً الگوی OwnerDashboardQueryService/CompanyDashboardQueryService.</summary>
public sealed class PublicStatsQueryService : IPublicStatsQueryService
{
    private readonly AppDbContext _dbContext;

    public PublicStatsQueryService(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<PublicStatsDto> GetPublicStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalVerifiedCompanies = await _dbContext.Companies.CountAsync(c => c.VerificationStatus == VerificationStatus.Verified, cancellationToken);
        var totalIndustrialZones = await _dbContext.IndustrialZones.CountAsync(cancellationToken);
        var totalCandidates = await _dbContext.Candidates.CountAsync(cancellationToken);
        var totalActiveJobAds = await _dbContext.JobAds.CountAsync(j => j.Status == JobAdStatus.Published, cancellationToken);

        return new PublicStatsDto(totalVerifiedCompanies, totalIndustrialZones, totalCandidates, totalActiveJobAds);
    }
}
