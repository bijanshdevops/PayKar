using IndustrialPlatform.Domain.Candidates;

namespace IndustrialPlatform.Application.Candidates;

public interface IBookmarkedJobRepository
{
    Task<BookmarkedJob?> GetByCandidateAndJobAdAsync(Guid candidateId, Guid jobAdId, CancellationToken cancellationToken = default);
    /// <summary>جدیدترین نشان‌شده‌ها ابتدا — برای صفحهٔ «آگهی‌های نشان‌شده».</summary>
    Task<IReadOnlyList<BookmarkedJob>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    void Add(BookmarkedJob bookmark);
    void Remove(BookmarkedJob bookmark);
}
