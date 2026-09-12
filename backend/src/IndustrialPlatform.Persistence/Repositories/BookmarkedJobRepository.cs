using IndustrialPlatform.Application.Candidates;
using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class BookmarkedJobRepository : IBookmarkedJobRepository
{
    private readonly AppDbContext _dbContext;

    public BookmarkedJobRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<BookmarkedJob?> GetByCandidateAndJobAdAsync(Guid candidateId, Guid jobAdId, CancellationToken cancellationToken = default) =>
        _dbContext.BookmarkedJobs.FirstOrDefaultAsync(b => b.CandidateId == candidateId && b.JobAdId == jobAdId, cancellationToken);

    public async Task<IReadOnlyList<BookmarkedJob>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) =>
        await _dbContext.BookmarkedJobs
            .Where(b => b.CandidateId == candidateId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(BookmarkedJob bookmark) => _dbContext.BookmarkedJobs.Add(bookmark);

    // طبق CLAUDE.md: حذف فیزیکی ممنوع — Remove() توسط AuditableEntitySaveChangesInterceptor
    // خودکار به Soft Delete (IsDeleted=true) تبدیل می‌شود.
    public void Remove(BookmarkedJob bookmark) => _dbContext.BookmarkedJobs.Remove(bookmark);
}
