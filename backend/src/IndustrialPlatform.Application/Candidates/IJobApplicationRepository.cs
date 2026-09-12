using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Api;

namespace IndustrialPlatform.Application.Candidates;

public interface IJobApplicationRepository
{
    Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JobApplication?> GetByTrackingTokenAsync(string trackingToken, CancellationToken cancellationToken = default);
    Task<bool> ExistsForCandidateAndJobAdAsync(Guid candidateId, Guid jobAdId, CancellationToken cancellationToken = default);
    Task<PagedResult<JobApplication>> GetByJobAdIdAsync(Guid jobAdId, int page, int pageSize, CancellationToken cancellationToken = default);
    /// <summary>تمام درخواست‌های یک آگهی بدون صفحه‌بندی — برای ویجت‌های داشبورد شرکت (تجمیع روی چند آگهی).</summary>
    Task<IReadOnlyList<JobApplication>> GetAllByJobAdIdAsync(Guid jobAdId, CancellationToken cancellationToken = default);
    /// <summary>طبق تسک #75 — تمام درخواست‌های ارسالی یک کارجو، جدیدترین در ابتدا.</summary>
    Task<IReadOnlyList<JobApplication>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    void Add(JobApplication jobApplication);
}
