namespace IndustrialPlatform.Domain.JobAds;

/// <summary>وضعیت خدمت سربازی — فیلد اختیاری، معمولاً فقط برای مشاغل آقایان تکمیل می‌شود.</summary>
public enum MilitaryServiceStatus
{
    NotApplicable = 1,
    Completed = 2,
    Exempted = 3,
    Deferred = 4
}
