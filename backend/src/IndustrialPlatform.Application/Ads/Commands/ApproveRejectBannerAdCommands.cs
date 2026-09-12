using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Ads.Commands;

/// <summary>
/// تایید بنر توسط ادمین/پنل Owner — طبق ADR-008.
/// گسترش‌یافته در فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۴): پیش از تایید، بررسی تداخل بازه
/// نمایش با سایر بنرهای Active همان جایگاه انجام می‌شود تا از نمایش همزمان دو بنر روی یک جایگاه جلوگیری شود.
/// </summary>
public sealed record ApproveBannerAdCommand(Guid BannerAdId) : IRequest<Result<BannerAdDto>>;

public sealed class ApproveBannerAdCommandHandler : IRequestHandler<ApproveBannerAdCommand, Result<BannerAdDto>>
{
    private readonly IBannerAdRepository _bannerAdRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ApproveBannerAdCommandHandler(IBannerAdRepository bannerAdRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _bannerAdRepository = bannerAdRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<BannerAdDto>> Handle(ApproveBannerAdCommand request, CancellationToken cancellationToken)
    {
        var bannerAd = await _bannerAdRepository.GetByIdAsync(request.BannerAdId, cancellationToken);
        if (bannerAd is null)
            return Result.Failure<BannerAdDto>(Error.NotFound("BANNER_AD_NOT_FOUND", "بنر مورد نظر یافت نشد."));

        var utcNow = _dateTimeProvider.UtcNow;

        if (bannerAd.BannerSlotId.HasValue)
        {
            var conflicting = await _bannerAdRepository.GetActiveConflictingBySlotAsync(bannerAd.BannerSlotId.Value, bannerAd.Id, utcNow, cancellationToken);
            if (conflicting is not null)
            {
                return Result.Failure<BannerAdDto>(Error.Conflict(
                    "BANNER_SLOT_OCCUPIED",
                    $"این جایگاه تا تاریخ {conflicting.EndDate:yyyy-MM-dd} در حال نمایش بنر دیگری است و امکان تایید همزمان وجود ندارد."));
            }
        }

        var result = bannerAd.Approve(utcNow);
        if (result.IsFailure)
            return Result.Failure<BannerAdDto>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(BannerAdMapper.ToDto(bannerAd));
    }
}

/// <summary>رد بنر توسط ادمین با ذکر دلیل — طبق ADR-008.</summary>
public sealed record RejectBannerAdCommand(Guid BannerAdId, string Reason) : IRequest<Result<BannerAdDto>>;

public sealed class RejectBannerAdCommandHandler : IRequestHandler<RejectBannerAdCommand, Result<BannerAdDto>>
{
    private readonly IBannerAdRepository _bannerAdRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectBannerAdCommandHandler(IBannerAdRepository bannerAdRepository, IUnitOfWork unitOfWork)
    {
        _bannerAdRepository = bannerAdRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BannerAdDto>> Handle(RejectBannerAdCommand request, CancellationToken cancellationToken)
    {
        var bannerAd = await _bannerAdRepository.GetByIdAsync(request.BannerAdId, cancellationToken);
        if (bannerAd is null)
            return Result.Failure<BannerAdDto>(Error.NotFound("BANNER_AD_NOT_FOUND", "بنر مورد نظر یافت نشد."));

        var result = bannerAd.Reject(request.Reason);
        if (result.IsFailure)
            return Result.Failure<BannerAdDto>(result.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(BannerAdMapper.ToDto(bannerAd));
    }
}
