using IndustrialPlatform.Domain.Candidates;

namespace IndustrialPlatform.Application.Candidates;

/// <summary>پورت خروجی مخزن CandidateWorkExperience — پیاده‌سازی واقعی در IndustrialPlatform.Persistence.</summary>
public interface ICandidateWorkExperienceRepository
{
    Task<IReadOnlyList<CandidateWorkExperience>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task<CandidateWorkExperience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(CandidateWorkExperience workExperience);
}
