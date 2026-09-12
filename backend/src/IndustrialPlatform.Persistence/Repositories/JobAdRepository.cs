using IndustrialPlatform.Application.JobAds;
using IndustrialPlatform.Domain.JobAds;
using IndustrialPlatform.Shared.Api;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class JobAdRepository : IJobAdRepository
{
    private readonly AppDbContext _dbContext;

    public JobAdRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<JobAd?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.JobAds.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<PagedResult<JobAd>> SearchPublishedAsync(
        JobAdSearchFilter filter, int page, int pageSize, string sortBy, string sortDir, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.JobAds.Where(j => j.Status == JobAdStatus.Published);

        if (filter.IndustrialZoneId.HasValue)
            query = query.Where(j => j.IndustrialZoneId == filter.IndustrialZoneId.Value);

        // جست‌وجو بر اساس شهر: از طریق پیوند با شهرک صنعتی مربوطه (JobAd فقط IndustrialZoneId دارد).
        if (filter.CityId.HasValue)
        {
            var cityId = filter.CityId.Value;
            query = query.Where(j => _dbContext.IndustrialZones.Any(z => z.Id == j.IndustrialZoneId && z.CityId == cityId));
        }

        if (filter.WorkShift.HasValue)
            query = query.Where(j => j.WorkShift == filter.WorkShift.Value);

        if (filter.HasCommuteService.HasValue)
            query = query.Where(j => j.HasCommuteService == filter.HasCommuteService.Value);

        if (filter.ContractType.HasValue)
            query = query.Where(j => j.ContractType == filter.ContractType.Value);

        if (filter.MinSalaryAmount.HasValue)
        {
            var minSalary = filter.MinSalaryAmount.Value;
            query = query.Where(j =>
                (j.SalaryRange.FixedAmount != null && j.SalaryRange.FixedAmount >= minSalary) ||
                (j.SalaryRange.MaxAmount != null && j.SalaryRange.MaxAmount >= minSalary));
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            query = query.Where(j => EF.Functions.ILike(j.Title, $"%{filter.Keyword}%"));

        query = (sortBy, sortDir?.ToLowerInvariant()) switch
        {
            ("title", "asc") => query.OrderBy(j => j.Title),
            ("title", _) => query.OrderByDescending(j => j.Title),
            (_, "asc") => query.OrderBy(j => j.CreatedAtUtc),
            _ => query.OrderByDescending(j => j.CreatedAtUtc)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return PagedResult<JobAd>.Create(items, totalCount, page, pageSize);
    }

    public async Task<PagedResult<JobAd>> GetByCompanyIdAsync(Guid companyId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.JobAds.Where(j => j.CompanyId == companyId).OrderByDescending(j => j.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return PagedResult<JobAd>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<JobAd>> GetAllByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _dbContext.JobAds
            .Where(j => j.CompanyId == companyId)
            .OrderByDescending(j => j.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    /// <summary>لیست آگهی‌های در انتظار بررسی — برای پنل Owner (استعلام/تایید آگهی).</summary>
    public async Task<PagedResult<JobAd>> GetPendingReviewAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.JobAds
            .Where(j => j.Status == JobAdStatus.PendingReview)
            .OrderBy(j => j.SubmittedForReviewAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return PagedResult<JobAd>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<JobAd>> GetExpiringPublishedAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.JobAds
            .Where(j => j.Status == JobAdStatus.Published && j.ExpiresAtUtc != null && j.ExpiresAtUtc <= utcNow)
            .ToListAsync(cancellationToken);

        return items;
    }

    public void Add(JobAd jobAd) => _dbContext.JobAds.Add(jobAd);
}
