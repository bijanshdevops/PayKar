using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.JobAds.Commands;

/// <summary>
/// انتقال خودکار آگهی‌های منقضی‌شده به وضعیت Expired — طبق docs/backend/Tasks.md فاز ۳.
/// توسط یک Background Job زمان‌بندی‌شده در لایه Api فراخوانی می‌شود (نه توسط کاربر).
/// </summary>
public sealed record ExpireJobAdsCommand : IRequest<Result<int>>;

public sealed class ExpireJobAdsCommandHandler : IRequestHandler<ExpireJobAdsCommand, Result<int>>
{
    private readonly IJobAdRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ExpireJobAdsCommandHandler(IJobAdRepository repository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<int>> Handle(ExpireJobAdsCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var expiringAds = await _repository.GetExpiringPublishedAsync(utcNow, cancellationToken);

        foreach (var jobAd in expiringAds)
        {
            jobAd.MarkExpiredIfNeeded(utcNow);
        }

        if (expiringAds.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(expiringAds.Count);
    }
}
