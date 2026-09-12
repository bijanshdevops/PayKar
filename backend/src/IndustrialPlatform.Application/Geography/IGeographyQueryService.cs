namespace IndustrialPlatform.Application.Geography;

/// <summary>
/// پورت خروجی خواندن داده‌های مرجع جغرافیایی (Read-Only) — پیاده‌سازی در Persistence.
/// چون این‌ها صرفاً داده Seed/Lookup هستند، از الگوی Repository کامل صرف‌نظر شده است.
/// </summary>
public interface IGeographyQueryService
{
    Task<IReadOnlyList<ProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CityDto>> GetCitiesAsync(Guid? provinceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndustrialZoneDto>> GetIndustrialZonesAsync(Guid? cityId, CancellationToken cancellationToken = default);
}
