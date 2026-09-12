using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Identity.Settings;
using IndustrialPlatform.Shared.Results;
using Microsoft.Extensions.Options;

namespace IndustrialPlatform.Identity.Services;

/// <summary>
/// منطق تولید/اعتبارسنجی OTP طبق سند 05-Security-Rules.md بخش ۲:
/// محدودیت نرخ درخواست، انقضا، و قفل موقت پس از تلاش‌های ناموفق.
/// کدها و شمارنده‌ها در Redis (ICacheService) نگهداری می‌شوند، نه در دیتابیس دائمی.
/// </summary>
public sealed class OtpService : IOtpService
{
    private readonly ICacheService _cache;
    private readonly ISmsService _smsService;
    private readonly OtpSettings _settings;

    private static string RateKey(string mobile) => $"otp:rate:{mobile}";
    private static string CodeKey(string mobile) => $"otp:code:{mobile}";
    private static string FailsKey(string mobile) => $"otp:fails:{mobile}";
    private static string LockKey(string mobile) => $"otp:lock:{mobile}";

    public OtpService(ICacheService cache, ISmsService smsService, IOptions<OtpSettings> settings)
    {
        _cache = cache;
        _smsService = smsService;
        _settings = settings.Value;
    }

    public async Task<Result<int>> RequestOtpAsync(string mobileNumber, CancellationToken cancellationToken = default)
    {
        var lockValue = await _cache.GetStringAsync(LockKey(mobileNumber), cancellationToken);
        if (lockValue is not null)
            return Result.Failure<int>(Error.Failure("RATE_LIMIT_EXCEEDED", "به دلیل تلاش‌های ناموفق زیاد، این شماره موقتاً مسدود شده است."));

        var requestCount = await _cache.IncrementAsync(
            RateKey(mobileNumber),
            TimeSpan.FromMinutes(_settings.RequestWindowMinutes),
            cancellationToken);

        if (requestCount > _settings.MaxRequestsPerWindow)
            return Result.Failure<int>(Error.Failure("RATE_LIMIT_EXCEEDED", "تعداد درخواست‌های کد تایید بیش از حد مجاز است. کمی بعد دوباره تلاش کنید."));

        var otpCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        await _cache.SetStringAsync(CodeKey(mobileNumber), otpCode, TimeSpan.FromSeconds(_settings.ExpirationSeconds), cancellationToken);
        await _cache.RemoveAsync(FailsKey(mobileNumber), cancellationToken);

        // یادداشت: فقط در محیط Development کد OTP در کنسول چاپ می‌شود تا بدون اطلاعات واقعی
        // ملی‌پیامک بتوان لاگین را تست کرد. طبق سند 05-Security-Rules.md، این کد هرگز نباید
        // در Production لاگ یا چاپ شود (اینجا صراحتاً به محیط Development محدود شده است).
        if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[DEV ONLY] کد OTP برای شماره {mobileNumber}: {otpCode}");
        }

        await _smsService.SendOtpAsync(mobileNumber, otpCode, cancellationToken);

        return Result.Success(_settings.ExpirationSeconds);
    }

    public async Task<Result> VerifyOtpAsync(string mobileNumber, string otpCode, CancellationToken cancellationToken = default)
    {
        var lockValue = await _cache.GetStringAsync(LockKey(mobileNumber), cancellationToken);
        if (lockValue is not null)
            return Result.Failure(Error.Failure("RATE_LIMIT_EXCEEDED", "این شماره موقتاً به دلیل تلاش‌های ناموفق مسدود شده است."));

        var storedCode = await _cache.GetStringAsync(CodeKey(mobileNumber), cancellationToken);

        if (storedCode is null || storedCode != otpCode)
        {
            var fails = await _cache.IncrementAsync(FailsKey(mobileNumber), TimeSpan.FromMinutes(_settings.RequestWindowMinutes), cancellationToken);

            if (fails >= _settings.MaxVerifyAttempts)
            {
                await _cache.SetStringAsync(LockKey(mobileNumber), "1", TimeSpan.FromMinutes(_settings.LockoutMinutes), cancellationToken);
            }

            return Result.Failure(Error.Validation("OTP_INVALID_OR_EXPIRED", "کد تایید نادرست یا منقضی شده است."));
        }

        await _cache.RemoveAsync(CodeKey(mobileNumber), cancellationToken);
        await _cache.RemoveAsync(FailsKey(mobileNumber), cancellationToken);

        return Result.Success();
    }
}
