namespace IndustrialPlatform.Domain.JobAds;

/// <summary>
/// چرخه حیات آگهی — طبق سند 02-Domain-Glossary.md بخش ۳.۱ (بازنگری‌شده برای جریان تایید ادمین).
/// Draft → (پرداخت هزینه ثابت) → PendingReview → (تایید ادمین) → Published
/// PendingReview → (رد ادمین) → Rejected → (اصلاح و ارسال مجدد) → PendingReview
/// </summary>
public enum JobAdStatus
{
    /// <summary>پیش‌نویس — هنوز هزینه پرداخت نشده و ارسال نشده است.</summary>
    Draft = 1,

    /// <summary>هزینه ثبت آگهی پرداخت شده و در صف بررسی و استعلام ادمین است.</summary>
    PendingReview = 2,

    /// <summary>پس از تایید ادمین منتشر و در جست‌وجوی عمومی قابل مشاهده است.</summary>
    Published = 3,

    /// <summary>توسط ادمین رد شده؛ شرکت می‌تواند اصلاح کرده و دوباره ارسال کند.</summary>
    Rejected = 4,

    /// <summary>مدت انتشار به پایان رسیده (Background Job).</summary>
    Expired = 5,

    /// <summary>توسط شرکت بسته شده است.</summary>
    Closed = 6,

    /// <summary>آرشیوشده.</summary>
    Archived = 7
}
