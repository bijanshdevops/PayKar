namespace IndustrialPlatform.Identity.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;

    // طبق تصمیم صریح محصولی: طول عمر Access Token باید دقیقاً ۸ ساعت (۴۸۰ دقیقه) باشد — این مقدار
    // پیش‌فرض صرفاً یک Fallback ایمنی است؛ مقدار واقعی از appsettings.json:Jwt:AccessTokenExpirationMinutes
    // خوانده می‌شود که منبع حقیقت (Single Source of Truth) طول عمر توکن است (JwtTokenService.GenerateAccessToken
    // و پارامتر ValidateLifetime=true در Program.cs این مقدار را در لحظه صدور و در هر درخواست اعتبارسنجی می‌کنند).
    public int AccessTokenExpirationMinutes { get; set; } = 480;
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
