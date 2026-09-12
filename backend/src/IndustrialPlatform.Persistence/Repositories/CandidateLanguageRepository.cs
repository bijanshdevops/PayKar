using IndustrialPlatform.Application.Candidates;
using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class CandidateLanguageRepository : ICandidateLanguageRepository
{
    private readonly AppDbContext _dbContext;

    public CandidateLanguageRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<CandidateLanguage>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default) =>
        await _dbContext.CandidateLanguages
            .Where(l => l.CandidateId == candidateId)
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);

    public Task<CandidateLanguage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CandidateLanguages.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(Guid candidateId, string name, CancellationToken cancellationToken = default) =>
        _dbContext.CandidateLanguages.AnyAsync(l => l.CandidateId == candidateId && l.Name == name, cancellationToken);

    public void Add(CandidateLanguage language) => _dbContext.CandidateLanguages.Add(language);
}
