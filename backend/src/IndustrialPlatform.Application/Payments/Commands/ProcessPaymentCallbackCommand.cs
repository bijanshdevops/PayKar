using IndustrialPlatform.Application.Ads;
using IndustrialPlatform.Application.Candidates;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.JobAds;
using IndustrialPlatform.Domain.Payments;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Payments.Commands;

/// <summary>
/// پردازش Callback زرین‌پال — طبق سند 05-Security-Rules.md بخش ۶: Verify همیشه سمت سرور
/// و بر اساس مبلغ ذخیره‌شده در تراکنش (نه مقدار ارسالی کلاینت) انجام می‌شود.
/// طبق ADR-008: بر اساس Purpose تراکنش، منطق تکمیل مربوطه (JobAd یا BannerAd) اجرا می‌شود.
/// طبق ADR-013: هدف سوم (ResumeCreationFee) نیز اکنون پشتیبانی می‌شود.
/// </summary>
public sealed record ProcessPaymentCallbackCommand(string Authority, string GatewayStatus) : IRequest<Result<PaymentCallbackResult>>;

public sealed class ProcessPaymentCallbackCommandHandler : IRequestHandler<ProcessPaymentCallbackCommand, Result<PaymentCallbackResult>>
{
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IJobAdRepository _jobAdRepository;
    private readonly IBannerAdRepository _bannerAdRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ProcessPaymentCallbackCommandHandler(
        IPaymentTransactionRepository transactionRepository,
        IJobAdRepository jobAdRepository,
        IBannerAdRepository bannerAdRepository,
        ICandidateRepository candidateRepository,
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _transactionRepository = transactionRepository;
        _jobAdRepository = jobAdRepository;
        _bannerAdRepository = bannerAdRepository;
        _candidateRepository = candidateRepository;
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PaymentCallbackResult>> Handle(ProcessPaymentCallbackCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByAuthorityAsync(request.Authority, cancellationToken);
        if (transaction is null)
            return Result.Failure<PaymentCallbackResult>(Error.NotFound("TRANSACTION_NOT_FOUND", "تراکنش مورد نظر یافت نشد."));

        if (!string.Equals(request.GatewayStatus, "OK", StringComparison.OrdinalIgnoreCase))
        {
            transaction.MarkFailed();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(new PaymentCallbackResult(false, null, "پرداخت توسط کاربر لغو شد.", transaction.JobAdId, transaction.BannerAdId, transaction.CandidateId));
        }

        var verifyResult = await _paymentGateway.VerifyPaymentAsync(transaction.Authority, transaction.AmountInRials, cancellationToken);
        if (!verifyResult.Success)
        {
            transaction.MarkFailed();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(new PaymentCallbackResult(false, null, verifyResult.ErrorMessage ?? "تایید تراکنش ناموفق بود.", transaction.JobAdId, transaction.BannerAdId, transaction.CandidateId));
        }

        var markResult = transaction.MarkSuccess(verifyResult.RefId ?? string.Empty);
        if (markResult.IsFailure)
            return Result.Success(new PaymentCallbackResult(true, transaction.RefId, "این تراکنش قبلاً پردازش شده بود.", transaction.JobAdId, transaction.BannerAdId, transaction.CandidateId));

        string successMessage;

        if (transaction.Purpose == PaymentPurpose.BannerAdFee && transaction.BannerAdId is not null)
        {
            var bannerAd = await _bannerAdRepository.GetByIdAsync(transaction.BannerAdId.Value, cancellationToken);
            if (bannerAd is not null)
            {
                bannerAd.MarkFeePaid();
                bannerAd.SubmitForReview(_dateTimeProvider.UtcNow);
            }

            successMessage = "پرداخت با موفقیت انجام شد. بنر تبلیغاتی شما برای بررسی و تایید نهایی ارسال شد.";
        }
        else if (transaction.Purpose == PaymentPurpose.ResumeCreationFee && transaction.CandidateId is not null)
        {
            var candidate = await _candidateRepository.GetByIdAsync(transaction.CandidateId.Value, cancellationToken);
            candidate?.MarkFeePaid();

            successMessage = "پرداخت با موفقیت انجام شد. رزومه شما نهایی شد.";
        }
        else if (transaction.JobAdId is not null)
        {
            var jobAd = await _jobAdRepository.GetByIdAsync(transaction.JobAdId.Value, cancellationToken);
            if (jobAd is not null)
            {
                jobAd.MarkFeePaid();
                jobAd.SubmitForReview(_dateTimeProvider.UtcNow);
            }

            successMessage = "پرداخت با موفقیت انجام شد. آگهی شما برای بررسی و تایید نهایی ارسال شد.";
        }
        else
        {
            successMessage = "پرداخت با موفقیت انجام شد.";
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PaymentCallbackResult(true, transaction.RefId, successMessage, transaction.JobAdId, transaction.BannerAdId, transaction.CandidateId));
    }
}
