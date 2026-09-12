using IndustrialPlatform.Domain.Candidates;

namespace IndustrialPlatform.Application.Candidates;

public interface ICandidateSkillRepository
{
    Task<IReadOnlyList<CandidateSkill>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task<CandidateSkill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(CandidateSkill skill);
}
