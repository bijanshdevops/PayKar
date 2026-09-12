using IndustrialPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace IndustrialPlatform.Api.Services;

/// <summary>
/// پیاده‌سازی IPaymentCallbackUrlProvider — آدرس بازگشت زرین‌پال را از تنظیمات
/// "PublicApiBaseUrl" (appsettings) می‌سازد. طبق سند 04-Api-Contract.md بخش ۵.۶:
/// GET {PublicApiBaseUrl}/api/v1/payments/callback
/// </summary>
public sealed class PaymentCallbackUrlProvider : IPaymentCallbackUrlProvider
{
    private readonly IConfiguration _configuration;

    public PaymentCallbackUrlProvider(IConfiguration configuration) => _configuration = configuration;

    public string GetCallbackUrl()
    {
        var baseUrl = _configuration["PublicApiBaseUrl"]
            ?? throw new InvalidOperationException("تنظیم 'PublicApiBaseUrl' در appsettings یافت نشد.");
        return $"{baseUrl.TrimEnd('/')}/api/v1/payments/callback";
    }
}
