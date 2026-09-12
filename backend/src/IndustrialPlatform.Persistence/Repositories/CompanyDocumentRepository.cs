using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Domain.Companies;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class CompanyDocumentRepository : ICompanyDocumentRepository
{
    private readonly AppDbContext _dbContext;

    public CompanyDocumentRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<CompanyDocument>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _dbContext.CompanyDocuments
            .Where(d => d.CompanyId == companyId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        _dbContext.CompanyDocuments.CountAsync(d => d.CompanyId == companyId, cancellationToken);

    public Task<CompanyDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.CompanyDocuments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public void Add(CompanyDocument document) => _dbContext.CompanyDocuments.Add(document);
}
