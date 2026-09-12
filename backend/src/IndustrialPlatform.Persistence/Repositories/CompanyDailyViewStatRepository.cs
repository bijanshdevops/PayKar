using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Domain.Companies;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class CompanyDailyViewStatRepository : ICompanyDailyViewStatRepository
{
    private readonly AppDbContext _dbContext;

    public CompanyDailyViewStatRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<CompanyDailyViewStat?> GetByCompanyAndDateAsync(Guid companyId, DateTime statDateUtc, CancellationToken cancellationToken = default) =>
        _dbContext.CompanyDailyViewStats.FirstOrDefaultAsync(
            s => s.CompanyId == companyId && s.StatDateUtc == statDateUtc.Date, cancellationToken);

    public void Add(CompanyDailyViewStat stat) => _dbContext.CompanyDailyViewStats.Add(stat);
}
