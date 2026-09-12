using IndustrialPlatform.Domain.Companies;
using IndustrialPlatform.Shared.Api;

namespace IndustrialPlatform.Application.Companies;

/// <summary>پورت خروجی مخزن Company — پیاده‌سازی واقعی در IndustrialPlatform.Persistence.</summary>
public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Company?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithNationalIdAsync(string nationalId, CancellationToken cancellationToken = default);
    Task<PagedResult<Company>> GetByVerificationStatusAsync(VerificationStatus status, int page, int pageSize, CancellationToken cancellationToken = default);
    void Add(Company company);
}
