namespace IndustrialPlatform.Application.Geography;

public sealed record ProvinceDto(Guid Id, string Name);
public sealed record CityDto(Guid Id, string Name, Guid ProvinceId);
public sealed record IndustrialZoneDto(Guid Id, string Name, Guid CityId, string ZoneType);
