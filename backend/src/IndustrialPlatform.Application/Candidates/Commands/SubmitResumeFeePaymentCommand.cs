using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Payments;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Domain.Payments;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// شروع پرداخت هزینه ثابت رزومه‌ساز (۲۵۰,۰۰۰ ریال) و هدایت به درگاه زرین‌پال — طبق ADR-013.
/// دقیقاً هم‌الگو با SubmitJobAdForReviewCommand، با این تفاوت که پرداخت‌کننده کارجو (Candidate) است، نه شرکت.
/// کارجو باید پیش از این فراخوانی، حداقل یک‌بار پروفایل/رزومه خود را از طریق SaveCandidateProfileCommand ذخیره کرده باشد.
/// </summary>
public sealed record SubmitResumeFeePaymentCommand : IRequest<Result<SubmitResumeFeePaymentResponse>>;

public sealed class SubmitResumeFeePaymentCommandHandler : IRequestHandler<SubmitResumeFeePaymentCommand, Result<SubmitResumeFeePaymentResponse>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IPaymentCallbackUrlProvider _callbackUrlProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SubmitResumeFeePaymentCommandHandler(
        ICandidateRepository candidateRepository,
        IPaymentTransactionRepository transactionRepository,
        IPaymentGateway paymentGateway,
        IPaymentCallbackUrlProvider callbackUrlProvider,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _candidateRepository = candidateRepository;
        _transactionRepository = transactionRepository;
        _paymentGateway = paymentGateway;
        _callbackUrlProvider = callbackUrlProvider;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<SubmitResumeFeePaymentResponse>> Handle(SubmitResumeFeePaymentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SubmitResumeFeePaymentResponse>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Failure<SubmitResumeFeePaymentResponse>(
                Error.NotFound("CANDIDATE_NOT_FOUND", "ابتدا اطلاعات رزومه خود را ذخیره کنید."));

        // اگر هزینه پیش‌تر پرداخت شده (یا این پروفایل از مسیر ارسال مستقیم/معاف ساخته شده)، نیازی به پرداخت مجدد نیست.
        if (candidate.IsFeePaid)
            return Result.Success(new SubmitResumeFeePaymentResponse(string.Empty, 0));

        var amountInRials = Candidate.ResumeCreationFeeAmountInRials;

        var paymentResult = await _paymentGateway.RequestPaymentAsync(
            amountInRials,
            "هزینه ساخت رزومه در رزومه‌ساز",
            _callbackUrlProvider.GetCallbackUrl(),
            cancellationToken);

        if (!paymentResult.Success || paymentResult.Authority is null || paymentResult.PaymentRedirectUrl is null)
            return Result.Failure<SubmitResumeFeePaymentResponse>(
                Error.Failure("PAYMENT_REQUEST_FAILED", paymentResult.ErrorMessage ?? "ثبت تراکنش پرداخت ناموفق بود."));

        var transaction = PaymentTransaction.CreateForResumeFee(candidate.Id, amountInRials, paymentResult.Authority);
        _transactionRepository.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SubmitResumeFeePaymentResponse(paymentResult.PaymentRedirectUrl, amountInRials));
    }
}
