namespace IndustrialPlatform.Application.Common.Interfaces;

/// <summary>پورت خروجی درگاه پرداخت — پیاده‌سازی واقعی (ZarinPal) در IndustrialPlatform.Infrastructure.</summary>
public interface IPaymentGateway
{
    Task<PaymentRequestResult> RequestPaymentAsync(long amountInRials, string description, string callbackUrl, CancellationToken cancellationToken = default);
    Task<PaymentVerificationResult> VerifyPaymentAsync(string authority, long amountInRials, CancellationToken cancellationToken = default);
}

public sealed record PaymentRequestResult(bool Success, string? Authority, string? PaymentRedirectUrl, string? ErrorMessage);

public sealed record PaymentVerificationResult(bool Success, string? RefId, string? ErrorMessage);
