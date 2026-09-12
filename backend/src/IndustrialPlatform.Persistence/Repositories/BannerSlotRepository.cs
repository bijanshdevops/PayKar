using IndustrialPlatform.Application.Ads;
using IndustrialPlatform.Domain.Ads;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class BannerSlotRepository : IBannerSlotRepository
{
    private readonly AppDbContext _dbContext;

    public BannerSlotRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<BannerSlot?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.BannerSlots.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BannerSlot>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.BannerSlots
            .Where(s => s.IsActive)
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);
}
