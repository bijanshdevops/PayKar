using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Application.Payments;
using IndustrialPlatform.Domain.Ads;
using IndustrialPlatform.Domain.Payments;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Ads.Commands;

/// <summary>
/// شروع پرداخت هزینه ثابت بنر و هدایت به درگاه زرین‌پال — طبق ADR-008
/// (عیناً الگوی SubmitJobAdForReviewCommand). پس از تایید موفق پرداخت در Callback،
/// بنر هم هزینه‌اش پرداخت‌شده علامت می‌خورد و هم مستقیماً به صف بررسی ادمین ارسال می‌شود.
/// </summary>
public sealed record SubmitBannerAdForReviewCommand(Guid BannerAdId) : IRequest<Result<SubmitBannerAdForReviewResponse>>;

public sealed class SubmitBannerAdForReviewCommandHandler : IRequestHandler<SubmitBannerAdForReviewCommand, Result<SubmitBannerAdForReviewResponse>>
{
    private readonly IBannerAdRepository _bannerAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IPaymentCallbackUrlProvider _callbackUrlProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SubmitBannerAdForReviewCommandHandler(
        IBannerAdRepository bannerAdRepository,
        ICompanyRepository companyRepository,
        IPaymentTransactionRepository transactionRepository,
        IPaymentGateway paymentGateway,
        IPaymentCallbackUrlProvider callbackUrlProvider,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _bannerAdRepository = bannerAdRepository;
        _companyRepository = companyRepository;
        _transactionRepository = transactionRepository;
        _paymentGateway = paymentGateway;
        _callbackUrlProvider = callbackUrlProvider;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<SubmitBannerAdForReviewResponse>> Handle(SubmitBannerAdForReviewCommand request, CancellationToken cancellationToken)
    {
        var bannerAd = await _bannerAdRepository.GetByIdAsync(request.BannerAdId, cancellationToken);
        if (bannerAd is null)
            return Result.Failure<SubmitBannerAdForReviewResponse>(Error.NotFound("BANNER_AD_NOT_FOUND", "بنر مورد نظر یافت نشد."));

        var ownershipCheck = await BannerAdOwnershipGuard.EnsureOwnerAsync(bannerAd, _companyRepository, _currentUser, cancellationToken);
        if (ownershipCheck.IsFailure)
            return Result.Failure<SubmitBannerAdForReviewResponse>(ownershipCheck.Error);

        if (bannerAd.Status is not (BannerAdStatus.Draft or BannerAdStatus.Rejected))
            return Result.Failure<SubmitBannerAdForReviewResponse>(
                Error.Conflict("BANNER_AD_NOT_SUBMITTABLE", "این بنر در وضعیتی نیست که قابل ارسال برای بررسی باشد."));

        if (bannerAd.IsFeePaid)
        {
            var directSubmitResult = bannerAd.SubmitForReview(DateTime.UtcNow);
            if (directSubmitResult.IsFailure)
                return Result.Failure<SubmitBannerAdForReviewResponse>(directSubmitResult.Error);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(new SubmitBannerAdForReviewResponse(string.Empty, 0));
        }

        if (bannerAd.TotalAmount <= 0)
            return Result.Failure<SubmitBannerAdForReviewResponse>(
                Error.Conflict("BANNER_AD_PRICE_NOT_SET", "ابتدا باید جایگاه و مدت زمان نمایش بنر انتخاب و مبلغ آن محاسبه شود."));

        var amountInRials = (long)bannerAd.TotalAmount;

        var paymentResult = await _paymentGateway.RequestPaymentAsync(
            amountInRials,
            "هزینه ثبت بنر تبلیغاتی",
            _callbackUrlProvider.GetCallbackUrl(),
            cancellationToken);

        if (!paymentResult.Success || paymentResult.Authority is null || paymentResult.PaymentRedirectUrl is null)
            return Result.Failure<SubmitBannerAdForReviewResponse>(
                Error.Failure("PAYMENT_REQUEST_FAILED", paymentResult.ErrorMessage ?? "ثبت تراکنش پرداخت ناموفق بود."));

        var transaction = PaymentTransaction.CreateForBannerAd(bannerAd.CompanyId, bannerAd.Id, amountInRials, paymentResult.Authority);
        _transactionRepository.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SubmitBannerAdForReviewResponse(paymentResult.PaymentRedirectUrl, amountInRials));
    }
}

public sealed record SubmitBannerAdForReviewResponse(string PaymentRedirectUrl, long AmountInRials);
