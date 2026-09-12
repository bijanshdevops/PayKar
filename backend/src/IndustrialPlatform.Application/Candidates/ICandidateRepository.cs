using IndustrialPlatform.Domain.Candidates;

namespace IndustrialPlatform.Application.Candidates;

public interface ICandidateRepository
{
    Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Candidate?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(Candidate candidate);
}
