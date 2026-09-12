using IndustrialPlatform.Application.Ads;
using IndustrialPlatform.Domain.Ads;
using IndustrialPlatform.Shared.Api;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class BannerAdRepository : IBannerAdRepository
{
    private readonly AppDbContext _dbContext;

    public BannerAdRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<BannerAd?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.BannerAds.Include(b => b.BannerSlot).FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<PagedResult<BannerAd>> GetByCompanyIdAsync(Guid companyId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BannerAds.Include(b => b.BannerSlot).Where(b => b.CompanyId == companyId).OrderByDescending(b => b.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return PagedResult<BannerAd>.Create(items, totalCount, page, pageSize);
    }

    public async Task<PagedResult<BannerAd>> GetPendingReviewAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BannerAds
            .Include(b => b.BannerSlot)
            .Where(b => b.Status == BannerAdStatus.PendingReview)
            .OrderBy(b => b.SubmittedForReviewAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return PagedResult<BannerAd>.Create(items, totalCount, page, pageSize);
    }

    public Task<BannerAd?> GetActiveConflictingBySlotAsync(int bannerSlotId, Guid excludeBannerAdId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        _dbContext.BannerAds
            .Where(b => b.Id != excludeBannerAdId
                        && b.BannerSlotId == bannerSlotId
                        && b.Status == BannerAdStatus.Active
                        && b.EndDate != null && b.EndDate > utcNow)
            .OrderByDescending(b => b.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

    public Task IncrementImpressionCountAsync(Guid bannerAdId, CancellationToken cancellationToken = default) =>
        _dbContext.BannerAds
            .Where(b => b.Id == bannerAdId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.ImpressionsCount, b => b.ImpressionsCount + 1), cancellationToken);

    public Task IncrementClickCountAsync(Guid bannerAdId, CancellationToken cancellationToken = default) =>
        _dbContext.BannerAds
            .Where(b => b.Id == bannerAdId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.ClicksCount, b => b.ClicksCount + 1), cancellationToken);

    public async Task<IReadOnlyList<BannerAd>> GetActiveByPlacementAsync(BannerPlacement placement, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await _dbContext.BannerAds
            .Where(b => b.Status == BannerAdStatus.Active
                        && (b.Placement == placement || b.Placement == BannerPlacement.Both)
                        && (b.ExpiresAtUtc == null || b.ExpiresAtUtc > utcNow))
            .OrderByDescending(b => b.ActivatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BannerAd>> GetExpiringActiveAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await _dbContext.BannerAds
            .Where(b => b.Status == BannerAdStatus.Active && b.ExpiresAtUtc != null && b.ExpiresAtUtc <= utcNow)
            .ToListAsync(cancellationToken);
    }

    public void Add(BannerAd bannerAd) => _dbContext.BannerAds.Add(bannerAd);
}
