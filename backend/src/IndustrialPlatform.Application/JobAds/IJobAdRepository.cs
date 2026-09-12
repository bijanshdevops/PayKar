using IndustrialPlatform.Domain.JobAds;
using IndustrialPlatform.Shared.Api;

namespace IndustrialPlatform.Application.JobAds;

/// <summary>فیلترهای جست‌وجوی پیشرفته آگهی — طبق درخواست «جست‌وجو بر اساس شهر/شهرک + فیلتر پیشرفته».</summary>
public sealed record JobAdSearchFilter(
    Guid? IndustrialZoneId,
    Guid? CityId,
    WorkShift? WorkShift,
    bool? HasCommuteService,
    ContractType? ContractType,
    decimal? MinSalaryAmount,
    string? Keyword);

/// <summary>پورت خروجی مخزن JobAd — پیاده‌سازی واقعی در IndustrialPlatform.Persistence.</summary>
public interface IJobAdRepository
{
    Task<JobAd?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<JobAd>> SearchPublishedAsync(JobAdSearchFilter filter, int page, int pageSize, string sortBy, string sortDir, CancellationToken cancellationToken = default);
    Task<PagedResult<JobAd>> GetByCompanyIdAsync(Guid companyId, int page, int pageSize, CancellationToken cancellationToken = default);
    /// <summary>تمام آگهی‌های یک شرکت بدون صفحه‌بندی — برای ویجت‌های داشبورد که باید روی همهٔ آگهی‌های شرکت تجمیع کنند.</summary>
    Task<IReadOnlyList<JobAd>> GetAllByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<PagedResult<JobAd>> GetPendingReviewAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobAd>> GetExpiringPublishedAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    void Add(JobAd jobAd);
}
