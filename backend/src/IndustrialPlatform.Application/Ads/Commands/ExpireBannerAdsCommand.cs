using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Ads.Commands;

/// <summary>انتقال خودکار بنرهای منقضی‌شده به وضعیت Expired — طبق ADR-008 (عیناً الگوی ExpireJobAdsCommand).</summary>
public sealed record ExpireBannerAdsCommand : IRequest<Result<int>>;

public sealed class ExpireBannerAdsCommandHandler : IRequestHandler<ExpireBannerAdsCommand, Result<int>>
{
    private readonly IBannerAdRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ExpireBannerAdsCommandHandler(IBannerAdRepository repository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<int>> Handle(ExpireBannerAdsCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var expiring = await _repository.GetExpiringActiveAsync(utcNow, cancellationToken);

        foreach (var bannerAd in expiring)
        {
            bannerAd.MarkExpiredIfNeeded(utcNow);
        }

        if (expiring.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(expiring.Count);
    }
}
