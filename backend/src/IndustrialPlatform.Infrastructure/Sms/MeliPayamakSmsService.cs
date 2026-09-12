using System.Net.Http.Json;
using IndustrialPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IndustrialPlatform.Infrastructure.Sms;

/// <summary>
/// آداپتور پیامک ملی‌پیامک — پیاده‌سازی پورت ISmsService (سند 01-Architecture، لایه Infrastructure).
/// از سرویس REST «ارسال پیامک با متن الگو/BodyId» ملی‌پیامک استفاده می‌کند.
/// یادداشت: قبل از استفاده در Production، Username/Password/BodyId واقعی را در appsettings
/// یا Secret Manager/Environment Variables تنظیم کنید (سند 05-Security-Rules.md بخش ۵).
/// </summary>
public sealed class MeliPayamakSmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly MeliPayamakSettings _settings;
    private readonly ILogger<MeliPayamakSmsService> _logger;

    public MeliPayamakSmsService(HttpClient httpClient, IOptions<MeliPayamakSettings> settings, ILogger<MeliPayamakSmsService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendOtpAsync(string mobileNumber, string otpCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                username = _settings.Username,
                password = _settings.Password,
                to = mobileNumber,
                bodyId = _settings.BodyId,
                text = otpCode
            };

            using var response = await _httpClient.PostAsJsonAsync("BaseServiceNumber", payload, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            // یادداشت: خطای ارسال پیامک نباید کل درخواست OTP کاربر را با Exception خام متوقف کند؛
            // Handler بالادستی (RequestOtpCommandHandler) بر اساس Result Pattern تصمیم می‌گیرد.
            _logger.LogError(ex, "ارسال پیامک OTP به شماره {MobileNumber} با خطا مواجه شد.", mobileNumber);
            throw;
        }
    }

    /// <summary>
    /// ارسال پیامک اطلاع‌رسانی Best-Effort — طبق قرارداد ISmsService.SendTextAsync هرگز نباید
    /// Exception پرتاب کند تا شکست پیامک، تراکنش اصلی (مثل تغییر وضعیت درخواست همکاری) را مختل نکند.
    /// </summary>
    public async Task SendTextAsync(string mobileNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                username = _settings.Username,
                password = _settings.Password,
                to = mobileNumber,
                bodyId = _settings.BodyId,
                text = message
            };

            using var response = await _httpClient.PostAsJsonAsync("BaseServiceNumber", payload, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ارسال پیامک اطلاع‌رسانی به شماره {MobileNumber} با خطا مواجه شد (نادیده گرفته شد).", mobileNumber);
        }
    }
}
