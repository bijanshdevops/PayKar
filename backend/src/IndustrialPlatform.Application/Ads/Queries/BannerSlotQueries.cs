using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Ads.Queries;

/// <summary>دریافت لیست جایگاه‌های فعال کاتالوگ تبلیغاتی — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۳).</summary>
public sealed record GetBannerSlotsQuery : IRequest<Result<IReadOnlyList<BannerSlotDto>>>;

public sealed class GetBannerSlotsQueryHandler : IRequestHandler<GetBannerSlotsQuery, Result<IReadOnlyList<BannerSlotDto>>>
{
    private readonly IBannerSlotRepository _bannerSlotRepository;

    public GetBannerSlotsQueryHandler(IBannerSlotRepository bannerSlotRepository) => _bannerSlotRepository = bannerSlotRepository;

    public async Task<Result<IReadOnlyList<BannerSlotDto>>> Handle(GetBannerSlotsQuery request, CancellationToken cancellationToken)
    {
        var slots = await _bannerSlotRepository.GetActiveAsync(cancellationToken);
        return Result.Success<IReadOnlyList<BannerSlotDto>>(slots.Select(BannerSlotMapper.ToDto).ToList());
    }
}
