using IndustrialPlatform.Domain.Candidates;

namespace IndustrialPlatform.Application.Candidates;

public interface ICandidateLanguageRepository
{
    Task<IReadOnlyList<CandidateLanguage>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task<CandidateLanguage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>برای پیش‌بررسی تکراری‌نبودن نام زبان پیش از افزودن (هم‌راستا با ایندکس یکتای candidate_id+name).</summary>
    Task<bool> ExistsByNameAsync(Guid candidateId, string name, CancellationToken cancellationToken = default);

    void Add(CandidateLanguage language);
}
