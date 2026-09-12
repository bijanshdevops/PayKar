namespace IndustrialPlatform.Identity.Features.Auth;

public sealed record RequestOtpResponse(int ExpiresInSeconds);

public sealed record AuthenticatedUserDto(Guid Id, string MobileNumber, IReadOnlyCollection<string> Roles);

public sealed record VerifyOtpResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    AuthenticatedUserDto User);

/// <summary>
/// User در پاسخ رفرش‌توکن گنجانده شده تا فرانت‌اند بتواند بلافاصله پس از تغییر نقش‌ها
/// (مثلاً پس از ثبت شرکت طبق ADR-010) بدون نیاز به خروج/ورود مجدد، نقش‌های به‌روز را دریافت کند.
/// </summary>
public sealed record RefreshTokenResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds, AuthenticatedUserDto User);

/// <summary>پاسخ ورود با نام‌کاربری/رمزعبور (ADR-005) — دقیقاً هم‌شکل با VerifyOtpResponse.</summary>
public sealed record LoginWithPasswordResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    AuthenticatedUserDto User);

public sealed record SetPasswordResponse(string Username);
