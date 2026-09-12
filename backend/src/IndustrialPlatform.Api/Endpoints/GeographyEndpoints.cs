using IndustrialPlatform.Api.Common;
using IndustrialPlatform.Application.Geography.Queries;
using MediatR;

namespace IndustrialPlatform.Api.Endpoints;

/// <summary>اندپوینت‌های داده مرجع جغرافیایی — بدون نیاز به احراز هویت (داده عمومی).</summary>
public static class GeographyEndpoints
{
    public static IEndpointRouteBuilder MapGeographyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/geography").WithTags("Geography");

        group.MapGet("/provinces", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProvincesQuery(), ct);
            return result.ToApiResult();
        });

        group.MapGet("/cities", async (Guid? provinceId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCitiesQuery(provinceId), ct);
            return result.ToApiResult();
        });

        group.MapGet("/industrial-zones", async (Guid? cityId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetIndustrialZonesQuery(cityId), ct);
            return result.ToApiResult();
        });

        return app;
    }
}
