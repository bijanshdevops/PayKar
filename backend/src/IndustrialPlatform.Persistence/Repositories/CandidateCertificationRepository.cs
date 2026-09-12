using IndustrialPlatform.Application.Candidates;
using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class CandidateCertificationRepository : ICandidateCertificationRepository
{
    private readonly AppDbContext _dbContext;

    public CandidateCertificationRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<CandidateCertification>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) =>
        await _dbContext.CandidateCertifications
            .Where(c => c.CandidateId == candidateId)
            .OrderByDescending(c => c.YearObtained)
            .ToListAsync(cancellationToken);

    public Task<CandidateCertification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CandidateCertifications.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(CandidateCertification certification) => _dbContext.CandidateCertifications.Add(certification);
}
