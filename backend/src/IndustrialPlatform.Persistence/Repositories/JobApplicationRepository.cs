using IndustrialPlatform.Application.Candidates;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Api;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class JobApplicationRepository : IJobApplicationRepository
{
    private readonly AppDbContext _dbContext;

    public JobApplicationRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.JobApplications.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<JobApplication?> GetByTrackingTokenAsync(string trackingToken, CancellationToken cancellationToken = default) =>
        _dbContext.JobApplications.FirstOrDefaultAsync(a => a.TrackingToken == trackingToken, cancellationToken);

    public Task<bool> ExistsForCandidateAndJobAdAsync(Guid candidateId, Guid jobAdId, CancellationToken cancellationToken = default) =>
        _dbContext.JobApplications.AnyAsync(a => a.CandidateId == candidateId && a.JobAdId == jobAdId, cancellationToken);

    public async Task<PagedResult<JobApplication>> GetByJobAdIdAsync(Guid jobAdId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.JobApplications.Where(a => a.JobAdId == jobAdId).OrderByDescending(a => a.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return PagedResult<JobApplication>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<JobApplication>> GetAllByJobAdIdAsync(Guid jobAdId, CancellationToken cancellationToken = default) =>
        await _dbContext.JobApplications
            .Where(a => a.JobAdId == jobAdId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<JobApplication>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) =>
        await _dbContext.JobApplications
            .Where(a => a.CandidateId == candidateId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(JobApplication jobApplication) => _dbContext.JobApplications.Add(jobApplication);
}
