using IndustrialPlatform.Domain.Candidates;

namespace IndustrialPlatform.Application.Candidates;

/// <summary>پورت خروجی مخزن CandidateEducation — پیاده‌سازی واقعی در IndustrialPlatform.Persistence.</summary>
public interface ICandidateEducationRepository
{
    Task<IReadOnlyList<CandidateEducation>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task<CandidateEducation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(CandidateEducation education);
}
