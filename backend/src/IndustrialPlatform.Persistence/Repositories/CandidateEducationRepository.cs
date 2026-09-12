using IndustrialPlatform.Application.Candidates;
using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class CandidateEducationRepository : ICandidateEducationRepository
{
    private readonly AppDbContext _dbContext;

    public CandidateEducationRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<CandidateEducation>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) =>
        await _dbContext.CandidateEducations
            .Where(e => e.CandidateId == candidateId)
            .OrderByDescending(e => e.GraduationYear)
            .ToListAsync(cancellationToken);

    public Task<CandidateEducation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CandidateEducations.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(CandidateEducation education) => _dbContext.CandidateEducations.Add(education);
}
