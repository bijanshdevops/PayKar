using IndustrialPlatform.Application.Candidates;
using IndustrialPlatform.Domain.Candidates;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class CandidateRepository : ICandidateRepository
{
    private readonly AppDbContext _dbContext;

    public CandidateRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Candidates.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Candidate?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.Candidates.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public void Add(Candidate candidate) => _dbContext.Candidates.Add(candidate);
}
