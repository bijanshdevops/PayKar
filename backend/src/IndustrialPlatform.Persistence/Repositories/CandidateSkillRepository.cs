using IndustrialPlatform.Application.Candidates;
using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class CandidateSkillRepository : ICandidateSkillRepository
{
    private readonly AppDbContext _dbContext;

    public CandidateSkillRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<CandidateSkill>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) =>
        await _dbContext.CandidateSkills
            .Where(s => s.CandidateId == candidateId)
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public Task<CandidateSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CandidateSkills.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(CandidateSkill skill) => _dbContext.CandidateSkills.Add(skill);
}
