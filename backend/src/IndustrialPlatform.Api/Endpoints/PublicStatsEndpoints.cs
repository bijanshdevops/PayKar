using IndustrialPlatform.Api.Common;
using IndustrialPlatform.Application.Dashboard.Queries;
using MediatR;

namespace IndustrialPlatform.Api.Endpoints;

/// <summary>آمار عمومی پلتفرم برای نوار اعتمادسازی صفحه اصلی — بدون نیاز به احراز هویت (داده عمومی).</summary>
public static class PublicStatsEndpoints
{
    public static IEndpointRouteBuilder MapPublicStatsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/public/stats", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPublicStatsQuery(), ct);
            return result.ToApiResult();
        }).WithTags("PublicStats");

        return app;
    }
}
