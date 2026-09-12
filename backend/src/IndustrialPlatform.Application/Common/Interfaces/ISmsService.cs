namespace IndustrialPlatform.Application.Common.Interfaces;

/// <summary>پورت خروجی پیامک — پیاده‌سازی واقعی (ملی‌پیامک) در IndustrialPlatform.Infrastructure.</summary>
public interface ISmsService
{
    Task SendOtpAsync(string mobileNumber, string otpCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// ارسال پیامک اطلاع‌رسانی (مثلاً تغییر وضعیت درخواست همکاری) — برخلاف SendOtpAsync،
    /// این متد Best-Effort است و هرگز نباید Exception پرتاب کند؛ شکست ارسال پیامک نباید
    /// تراکنش بیزینسی اصلی (مثل ثبت درخواست یا تغییر وضعیت آن) را مختل کند.
    /// </summary>
    Task SendTextAsync(string mobileNumber, string message, CancellationToken cancellationToken = default);
}
