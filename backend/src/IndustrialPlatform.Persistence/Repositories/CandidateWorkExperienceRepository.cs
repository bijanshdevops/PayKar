using IndustrialPlatform.Application.Candidates;
using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class CandidateWorkExperienceRepository : ICandidateWorkExperienceRepository
{
    private readonly AppDbContext _dbContext;

    public CandidateWorkExperienceRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<CandidateWorkExperience>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) =>
        await _dbContext.CandidateWorkExperiences
            .Where(w => w.CandidateId == candidateId)
            .OrderByDescending(w => w.StartYear)
            .ToListAsync(cancellationToken);

    public Task<CandidateWorkExperience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CandidateWorkExperiences.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public void Add(CandidateWorkExperience workExperience) => _dbContext.CandidateWorkExperiences.Add(workExperience);
}
