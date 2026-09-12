using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Domain.Companies;
using IndustrialPlatform.Shared.Api;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly AppDbContext _dbContext;

    public CompanyRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Company?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
        _dbContext.Companies.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId, cancellationToken);

    public Task<bool> ExistsWithNationalIdAsync(string nationalId, CancellationToken cancellationToken = default) =>
        _dbContext.Companies.AnyAsync(c => c.NationalId == nationalId, cancellationToken);

    public async Task<PagedResult<Company>> GetByVerificationStatusAsync(
        VerificationStatus status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Companies.Where(c => c.VerificationStatus == status).OrderByDescending(c => c.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return PagedResult<Company>.Create(items, totalCount, page, pageSize);
    }

    public void Add(Company company) => _dbContext.Companies.Add(company);
}
