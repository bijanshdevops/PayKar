using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Application.Payments;
using IndustrialPlatform.Domain.Ads;
using IndustrialPlatform.Domain.Payments;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Ads.Commands;

/// <summary>
/// تمدید بنر تبلیغاتی با ۲۰٪ تخفیف و کسر مستقیم از کیف‌پول شرکت — طبق فاز «مدیریت و رزرو بنرهای
/// تبلیغاتی» (تسک ۵). قیمت روزانه همیشه از رکورد واقعی BannerSlot متصل به بنر خوانده می‌شود؛
/// هیچ مبلغ فیکی محاسبه/فرض نمی‌شود.
/// </summary>
public sealed record RenewBannerAdCommand(Guid BannerAdId, int? DurationDays = null) : IRequest<Result<BannerAdDto>>;

public sealed class RenewBannerAdCommandValidator : AbstractValidator<RenewBannerAdCommand>
{
    public RenewBannerAdCommandValidator()
    {
        RuleFor(x => x.DurationDays).GreaterThan(0).When(x => x.DurationDays.HasValue)
            .WithMessage("مدت زمان تمدید باید بزرگ‌تر از صفر باشد.");
    }
}

public sealed class RenewBannerAdCommandHandler : IRequestHandler<RenewBannerAdCommand, Result<BannerAdDto>>
{
    private const decimal RenewalDiscountMultiplier = 0.80m;

    private readonly IBannerAdRepository _bannerAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RenewBannerAdCommandHandler(
        IBannerAdRepository bannerAdRepository,
        ICompanyRepository companyRepository,
        IPaymentTransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _bannerAdRepository = bannerAdRepository;
        _companyRepository = companyRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<BannerAdDto>> Handle(RenewBannerAdCommand request, CancellationToken cancellationToken)
    {
        var bannerAd = await _bannerAdRepository.GetByIdAsync(request.BannerAdId, cancellationToken);
        if (bannerAd is null)
            return Result.Failure<BannerAdDto>(Error.NotFound("BANNER_AD_NOT_FOUND", "بنر مورد نظر یافت نشد."));

        var ownershipCheck = await BannerAdOwnershipGuard.EnsureOwnerAsync(bannerAd, _companyRepository, _currentUser, cancellationToken);
        if (ownershipCheck.IsFailure)
            return Result.Failure<BannerAdDto>(ownershipCheck.Error);

        if (bannerAd.Status is not (BannerAdStatus.Active or BannerAdStatus.Expired))
            return Result.Failure<BannerAdDto>(Error.Conflict("BANNER_AD_NOT_RENEWABLE", "فقط بنرهای فعال یا منقضی‌شده قابل تمدید هستند."));

        if (bannerAd.BannerSlotId is null || bannerAd.BannerSlot is null)
            return Result.Failure<BannerAdDto>(Error.Conflict("BANNER_SLOT_MISSING", "این بنر به جایگاه قیمت‌گذاری متصل نیست و قابل تمدید نیست."));

        var utcNow = _dateTimeProvider.UtcNow;
        var durationDays = request.DurationDays ?? (bannerAd.DurationDays > 0 ? bannerAd.DurationDays : BannerAd.DefaultDurationDays);

        // طبق تسک ۴: اگر بنر منقضی شده، بازه جدید از اکنون آغاز می‌شود — پس باید مجدداً بررسی تداخل جایگاه انجام شود.
        if (bannerAd.Status == BannerAdStatus.Expired)
        {
            var conflicting = await _bannerAdRepository.GetActiveConflictingBySlotAsync(bannerAd.BannerSlotId.Value, bannerAd.Id, utcNow, cancellationToken);
            if (conflicting is not null)
            {
                return Result.Failure<BannerAdDto>(Error.Conflict(
                    "BANNER_SLOT_OCCUPIED",
                    $"این جایگاه تا تاریخ {conflicting.EndDate:yyyy-MM-dd} در حال نمایش بنر دیگری است و امکان تمدید همزمان وجود ندارد."));
            }
        }

        var baseAmount = bannerAd.BannerSlot.DailyPrice * durationDays;
        var discountedAmount = Math.Round(baseAmount * RenewalDiscountMultiplier, 0, MidpointRounding.AwayFromZero);
        var amountInRials = (long)discountedAmount;

        var company = await _companyRepository.GetByIdAsync(bannerAd.CompanyId, cancellationToken);
        if (company is null)
            return Result.Failure<BannerAdDto>(Error.NotFound("COMPANY_NOT_FOUND", "شرکت مالک این بنر یافت نشد."));

        var debitResult = company.DebitWallet(amountInRials);
        if (debitResult.IsFailure)
            return Result.Failure<BannerAdDto>(debitResult.Error);

        var renewResult = bannerAd.Renew(durationDays, discountedAmount, utcNow);
        if (renewResult.IsFailure)
            return Result.Failure<BannerAdDto>(renewResult.Error);

        var description = $"تمدید بنر تبلیغاتی {bannerAd.Id} - با ۲۰٪ تخفیف";
        var transaction = PaymentTransaction.CreateSucceededForWalletDebit(company.Id, bannerAd.Id, amountInRials, PaymentPurpose.BannerAdRenewal, description);
        _transactionRepository.Add(transaction);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(BannerAdMapper.ToDto(bannerAd));
    }
}
