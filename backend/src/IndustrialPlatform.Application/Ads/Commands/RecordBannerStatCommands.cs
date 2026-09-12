using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Ads.Commands;

/// <summary>
/// ثبت یک بازدید (Impression) بنر — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۶).
/// اندپوینت عمومی (بدون احراز هویت) — پیکسل/تگ تصویر روی صفحات نمایش‌دهنده بنر این را صدا می‌زند.
/// </summary>
public sealed record RecordBannerImpressionCommand(Guid BannerAdId) : IRequest<Result>;

public sealed class RecordBannerImpressionCommandHandler : IRequestHandler<RecordBannerImpressionCommand, Result>
{
    private readonly IBannerAdRepository _bannerAdRepository;
    private readonly IBannerDailyStatRepository _bannerDailyStatRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordBannerImpressionCommandHandler(
        IBannerAdRepository bannerAdRepository, IBannerDailyStatRepository bannerDailyStatRepository, IDateTimeProvider dateTimeProvider)
    {
        _bannerAdRepository = bannerAdRepository;
        _bannerDailyStatRepository = bannerDailyStatRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RecordBannerImpressionCommand request, CancellationToken cancellationToken)
    {
        var bannerAd = await _bannerAdRepository.GetByIdAsync(request.BannerAdId, cancellationToken);
        if (bannerAd is null || !bannerAd.IsDisplayable())
            return Result.Failure(Error.NotFound("BANNER_AD_NOT_FOUND", "بنر مورد نظر یافت نشد یا در حال نمایش نیست."));

        var utcNow = _dateTimeProvider.UtcNow;

        // یادداشت معماری: این دو افزایش به‌صورت مستقیم و اتمیک در دیتابیس اعمال می‌شوند (ExecuteUpdateAsync)
        // و بلافاصله Commit می‌شوند — برخلاف الگوی معمول این پروژه، به IUnitOfWork.SaveChangesAsync
        // نیازی نیست، چون خودِ این عملیات‌ها هیچ موجودیتی را در ChangeTracker بارگذاری/ردیابی نمی‌کنند.
        await _bannerAdRepository.IncrementImpressionCountAsync(bannerAd.Id, cancellationToken);
        await _bannerDailyStatRepository.IncrementImpressionAsync(bannerAd.Id, utcNow, cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// ثبت یک کلیک بنر و بازگرداندن لینک مقصد برای Redirect — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۶).
/// </summary>
public sealed record RecordBannerClickCommand(Guid BannerAdId) : IRequest<Result<string>>;

public sealed class RecordBannerClickCommandHandler : IRequestHandler<RecordBannerClickCommand, Result<string>>
{
    private readonly IBannerAdRepository _bannerAdRepository;
    private readonly IBannerDailyStatRepository _bannerDailyStatRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordBannerClickCommandHandler(
        IBannerAdRepository bannerAdRepository, IBannerDailyStatRepository bannerDailyStatRepository, IDateTimeProvider dateTimeProvider)
    {
        _bannerAdRepository = bannerAdRepository;
        _bannerDailyStatRepository = bannerDailyStatRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<string>> Handle(RecordBannerClickCommand request, CancellationToken cancellationToken)
    {
        var bannerAd = await _bannerAdRepository.GetByIdAsync(request.BannerAdId, cancellationToken);
        if (bannerAd is null || !bannerAd.IsDisplayable())
            return Result.Failure<string>(Error.NotFound("BANNER_AD_NOT_FOUND", "بنر مورد نظر یافت نشد یا در حال نمایش نیست."));

        var utcNow = _dateTimeProvider.UtcNow;

        await _bannerAdRepository.IncrementClickCountAsync(bannerAd.Id, cancellationToken);
        await _bannerDailyStatRepository.IncrementClickAsync(bannerAd.Id, utcNow, cancellationToken);

        return Result.Success(bannerAd.DestinationUrl);
    }
}
