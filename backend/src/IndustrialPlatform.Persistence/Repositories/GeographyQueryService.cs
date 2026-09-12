using IndustrialPlatform.Application.Geography;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Persistence.Repositories;

public sealed class GeographyQueryService : IGeographyQueryService
{
    private readonly AppDbContext _dbContext;

    public GeographyQueryService(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Provinces
            .OrderBy(p => p.Name)
            .Select(p => new ProvinceDto(p.Id, p.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CityDto>> GetCitiesAsync(Guid? provinceId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Cities.AsQueryable();
        if (provinceId.HasValue) query = query.Where(c => c.ProvinceId == provinceId.Value);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CityDto(c.Id, c.Name, c.ProvinceId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IndustrialZoneDto>> GetIndustrialZonesAsync(Guid? cityId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.IndustrialZones.AsQueryable();
        if (cityId.HasValue) query = query.Where(z => z.CityId == cityId.Value);

        return await query
            .OrderBy(z => z.Name)
            .Select(z => new IndustrialZoneDto(z.Id, z.Name, z.CityId, z.ZoneType.ToString()))
            .ToListAsync(cancellationToken);
    }
}
