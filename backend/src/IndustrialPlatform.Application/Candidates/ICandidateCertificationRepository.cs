using IndustrialPlatform.Domain.Candidates;

namespace IndustrialPlatform.Application.Candidates;

public interface ICandidateCertificationRepository
{
    Task<IReadOnlyList<CandidateCertification>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task<CandidateCertification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(CandidateCertification certification);
}
