namespace IndustrialPlatform.Domain.Ads;

/// <summary>
/// چرخه حیات بنر تبلیغاتی — طبق ADR-008: عیناً الگوی JobAdStatus.
/// Draft → (پرداخت هزینه ثابت) → PendingReview → (تایید ادمین) → Active
/// PendingReview → (رد ادمین) → Rejected → (اصلاح و ارسال مجدد) → PendingReview
/// Active → (انقضای خودکار Background Job) → Expired
/// </summary>
public enum BannerAdStatus
{
    Draft = 1,
    PendingReview = 2,
    Active = 3,
    Rejected = 4,
    Expired = 5
}
