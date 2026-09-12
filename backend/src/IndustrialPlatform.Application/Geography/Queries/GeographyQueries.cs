using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Geography.Queries;

public sealed record GetProvincesQuery : IRequest<Result<IReadOnlyList<ProvinceDto>>>;
public sealed record GetCitiesQuery(Guid? ProvinceId) : IRequest<Result<IReadOnlyList<CityDto>>>;
public sealed record GetIndustrialZonesQuery(Guid? CityId) : IRequest<Result<IReadOnlyList<IndustrialZoneDto>>>;

public sealed class GetProvincesQueryHandler : IRequestHandler<GetProvincesQuery, Result<IReadOnlyList<ProvinceDto>>>
{
    private readonly IGeographyQueryService _service;
    public GetProvincesQueryHandler(IGeographyQueryService service) => _service = service;

    public async Task<Result<IReadOnlyList<ProvinceDto>>> Handle(GetProvincesQuery request, CancellationToken cancellationToken) =>
        Result.Success(await _service.GetProvincesAsync(cancellationToken));
}

public sealed class GetCitiesQueryHandler : IRequestHandler<GetCitiesQuery, Result<IReadOnlyList<CityDto>>>
{
    private readonly IGeographyQueryService _service;
    public GetCitiesQueryHandler(IGeographyQueryService service) => _service = service;

    public async Task<Result<IReadOnlyList<CityDto>>> Handle(GetCitiesQuery request, CancellationToken cancellationToken) =>
        Result.Success(await _service.GetCitiesAsync(request.ProvinceId, cancellationToken));
}

public sealed class GetIndustrialZonesQueryHandler : IRequestHandler<GetIndustrialZonesQuery, Result<IReadOnlyList<IndustrialZoneDto>>>
{
    private readonly IGeographyQueryService _service;
    public GetIndustrialZonesQueryHandler(IGeographyQueryService service) => _service = service;

    public async Task<Result<IReadOnlyList<IndustrialZoneDto>>> Handle(GetIndustrialZonesQuery request, CancellationToken cancellationToken) =>
        Result.Success(await _service.GetIndustrialZonesAsync(request.CityId, cancellationToken));
}
