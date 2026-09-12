using IndustrialPlatform.Domain.Companies;

namespace IndustrialPlatform.Application.Companies;

/// <summary>پورت خروجی مخزن CompanyDocument — پیاده‌سازی واقعی در IndustrialPlatform.Persistence.</summary>
public interface ICompanyDocumentRepository
{
    Task<IReadOnlyList<CompanyDocument>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<CompanyDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(CompanyDocument document);
}
