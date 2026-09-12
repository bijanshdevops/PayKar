namespace IndustrialPlatform.Application.Dashboard;

/// <summary>
/// آمار عمومی پلتفرم برای نوار اعتمادسازی صفحه اصلی (بدون نیاز به احراز هویت).
/// فقط شامل شمارش‌های تجمیعی بی‌ضرر است — هیچ داده شخصی/تجاری حساسی افشا نمی‌شود.
/// </summary>
public sealed record PublicStatsDto(
    int TotalVerifiedCompanies,
    int TotalIndustrialZones,
    int TotalCandidates,
    int TotalActiveJobAds);
