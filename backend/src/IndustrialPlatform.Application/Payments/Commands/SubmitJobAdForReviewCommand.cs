using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Application.JobAds;
using IndustrialPlatform.Domain.JobAds;
using IndustrialPlatform.Domain.Payments;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Payments.Commands;

/// <summary>
/// شروع پرداخت هزینه ثابت ثبت آگهی (۵۵۰,۰۰۰ ریال) و هدایت به درگاه زرین‌پال.
/// آگهی باید متعلق به شرکتِ کاربر جاری و در وضعیت Draft یا Rejected باشد.
/// پس از تایید موفق پرداخت در Callback، آگهی به‌صورت خودکار هم هزینه‌اش پرداخت‌شده علامت می‌خورد
/// و هم مستقیماً به صف بررسی ادمین (PendingReview) ارسال می‌شود.
/// </summary>
public sealed record SubmitJobAdForReviewCommand(Guid JobAdId) : IRequest<Result<SubmitJobAdForReviewResponse>>;

public sealed class SubmitJobAdForReviewCommandValidator : AbstractValidator<SubmitJobAdForReviewCommand>
{
    public SubmitJobAdForReviewCommandValidator()
    {
        RuleFor(x => x.JobAdId).NotEmpty();
    }
}

public sealed class SubmitJobAdForReviewCommandHandler : IRequestHandler<SubmitJobAdForReviewCommand, Result<SubmitJobAdForReviewResponse>>
{
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IPaymentCallbackUrlProvider _callbackUrlProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SubmitJobAdForReviewCommandHandler(
        IJobAdRepository jobAdRepository,
        ICompanyRepository companyRepository,
        IPaymentTransactionRepository transactionRepository,
        IPaymentGateway paymentGateway,
        IPaymentCallbackUrlProvider callbackUrlProvider,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _transactionRepository = transactionRepository;
        _paymentGateway = paymentGateway;
        _callbackUrlProvider = callbackUrlProvider;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<SubmitJobAdForReviewResponse>> Handle(SubmitJobAdForReviewCommand request, CancellationToken cancellationToken)
    {
        var jobAd = await _jobAdRepository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<SubmitJobAdForReviewResponse>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        var ownershipCheck = await JobAdOwnershipGuard.EnsureOwnerAsync(jobAd, _companyRepository, _currentUser, cancellationToken);
        if (ownershipCheck.IsFailure)
            return Result.Failure<SubmitJobAdForReviewResponse>(ownershipCheck.Error);

        if (jobAd.Status is not (JobAdStatus.Draft or JobAdStatus.Rejected))
            return Result.Failure<SubmitJobAdForReviewResponse>(
                Error.Conflict("JOB_AD_NOT_SUBMITTABLE", "این آگهی در وضعیتی نیست که قابل ارسال برای بررسی باشد."));

        // اگر هزینه پیش‌تر پرداخت شده (مثلاً پس از رد ادمین و اصلاح آگهی)، نیازی به پرداخت مجدد نیست
        // و مستقیماً به صف بررسی ارسال می‌شود.
        if (jobAd.IsFeePaid)
        {
            var directSubmitResult = jobAd.SubmitForReview(DateTime.UtcNow);
            if (directSubmitResult.IsFailure)
                return Result.Failure<SubmitJobAdForReviewResponse>(directSubmitResult.Error);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(new SubmitJobAdForReviewResponse(string.Empty, 0));
        }

        var amountInRials = JobAd.ListingFeeAmountInRials;

        var paymentResult = await _paymentGateway.RequestPaymentAsync(
            amountInRials,
            $"هزینه ثبت آگهی «{jobAd.Title}»",
            _callbackUrlProvider.GetCallbackUrl(),
            cancellationToken);

        if (!paymentResult.Success || paymentResult.Authority is null || paymentResult.PaymentRedirectUrl is null)
            return Result.Failure<SubmitJobAdForReviewResponse>(
                Error.Failure("PAYMENT_REQUEST_FAILED", paymentResult.ErrorMessage ?? "ثبت تراکنش پرداخت ناموفق بود."));

        var transaction = PaymentTransaction.Create(jobAd.CompanyId, jobAd.Id, amountInRials, paymentResult.Authority, PaymentPurpose.JobAdListingFee);
        _transactionRepository.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SubmitJobAdForReviewResponse(paymentResult.PaymentRedirectUrl, amountInRials));
    }
}
