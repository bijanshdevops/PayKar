using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Candidates;

/// <summary>
/// نشان‌کردن یک آگهی توسط کارجو («آگهی‌های نشان‌شده» — طبق 02_home_page_spec.md بخش ۲.۴ و
/// 03_jobseeker_dashboard_spec.md بخش ۱). موجودیت ساده/بدون چرخهٔ حیات پیچیده — صرفاً یک نشانگر
/// رابطهٔ کارجو↔آگهی، مشابه الگوی JobApplication. حذف («لغو نشان») از طریق Soft Delete استاندارد
/// پروژه انجام می‌شود (AuditableEntitySaveChangesInterceptor)، نه حذف فیزیکی.
/// </summary>
public sealed class BookmarkedJob : AggregateRoot<Guid>
{
    public Guid CandidateId { get; private set; }
    public Guid JobAdId { get; private set; }

    private BookmarkedJob() { }

    private BookmarkedJob(Guid id, Guid candidateId, Guid jobAdId) : base(id)
    {
        CandidateId = candidateId;
        JobAdId = jobAdId;
    }

    public static BookmarkedJob Create(Guid candidateId, Guid jobAdId) =>
        new(Guid.NewGuid(), candidateId, jobAdId);
}
