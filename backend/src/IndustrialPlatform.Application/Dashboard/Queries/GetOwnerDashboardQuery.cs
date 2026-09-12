using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Dashboard.Queries;

/// <summary>دریافت آمار داشبورد پنل Owner — طبق ADR-009. دسترسی صرفاً AdminOnly (در سطح Endpoint).</summary>
public sealed record GetOwnerDashboardQuery(int TimeSeriesDays = 30) : IRequest<Result<OwnerDashboardDto>>;

public sealed class GetOwnerDashboardQueryHandler : IRequestHandler<GetOwnerDashboardQuery, Result<OwnerDashboardDto>>
{
    private readonly IOwnerDashboardQueryService _dashboardQueryService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetOwnerDashboardQueryHandler(IOwnerDashboardQueryService dashboardQueryService, IDateTimeProvider dateTimeProvider)
    {
        _dashboardQueryService = dashboardQueryService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<OwnerDashboardDto>> Handle(GetOwnerDashboardQuery request, CancellationToken cancellationToken)
    {
        var days = request.TimeSeriesDays is > 0 and <= 365 ? request.TimeSeriesDays : 30;
        var dashboard = await _dashboardQueryService.GetDashboardAsync(_dateTimeProvider.UtcNow, days, cancellationToken);
        return Result.Success(dashboard);
    }
}
