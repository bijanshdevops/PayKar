using IndustrialPlatform.Api.Common;
using IndustrialPlatform.Application.Dashboard.Queries;
using MediatR;

namespace IndustrialPlatform.Api.Endpoints;

/// <summary>اندپوینت داشبورد آمار و آنالیز پنل Owner — طبق ADR-009 سند 08-Decision-Log.md.</summary>
public static class OwnerDashboardEndpoints
{
    public static IEndpointRouteBuilder MapOwnerDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/dashboard").WithTags("Admin-Dashboard").RequireAuthorization("AdminOnly");

        group.MapGet("/", async (int? timeSeriesDays, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOwnerDashboardQuery(timeSeriesDays is > 0 ? timeSeriesDays.Value : 30), ct);
            return result.ToApiResult();
        });

        return app;
    }
}
