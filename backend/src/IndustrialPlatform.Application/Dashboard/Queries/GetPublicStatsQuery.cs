using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Dashboard.Queries;

/// <summary>آمار عمومی پلتفرم برای صفحه اصلی — بدون نیاز به احراز هویت (Endpoint عمومی).</summary>
public sealed record GetPublicStatsQuery : IRequest<Result<PublicStatsDto>>;

public sealed class GetPublicStatsQueryHandler : IRequestHandler<GetPublicStatsQuery, Result<PublicStatsDto>>
{
    private readonly IPublicStatsQueryService _statsQueryService;

    public GetPublicStatsQueryHandler(IPublicStatsQueryService statsQueryService) => _statsQueryService = statsQueryService;

    public async Task<Result<PublicStatsDto>> Handle(GetPublicStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = await _statsQueryService.GetPublicStatsAsync(cancellationToken);
        return Result.Success(stats);
    }
}
