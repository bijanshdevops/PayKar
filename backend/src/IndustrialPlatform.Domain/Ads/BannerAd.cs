using IndustrialPlatform.Shared.Entities;
using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Domain.Ads;

/// <summary>
/// بنر تبلیغاتی خریداری‌شده توسط شرکت‌ها برای نمایش در صفحه اصلی و/یا لیست آگهی‌ها — طبق ADR-008.
/// چرخه حیات و منطق پرداخت/تایید عیناً از الگوی JobAd.cs پیروی می‌کند.
/// گسترش‌یافته در فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۲): جایگزینی هزینه ثابت واحد با
/// کاتالوگ جایگاه (BannerSlot) و قیمت‌گذاری روزانه، بازه زمانی رزرو، و شمارنده‌های اولیه نمایش/کلیک.
/// BannerSlotId فعلاً Nullable است تا مسیر ایجاد قدیمی (CreateBannerAdCommand، تسک بعدی) بدون تغییر
/// کامپایل شود؛ سخت‌گیری آن به NOT NULL و اتصال کامل جریان انتخاب جایگاه در تسک‌های بعدی این فاز انجام می‌شود.
/// </summary>
public sealed class BannerAd : AggregateRoot<Guid>
{
    /// <summary>
    /// هزینه ثابت قدیمی (پیش از کاتالوگ جایگاه‌ها) — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی»،
    /// جایگزین آن BannerSlot.DailyPrice × BannerAd.DurationDays (فیلد TotalAmount) شده است.
    /// تا زمان بازنویسی کامل جریان پرداخت (تسک‌های بعدی)، همچنان توسط SubmitBannerAdForReviewCommand
    /// خوانده می‌شود؛ لذا هنوز حذف نشده، صرفاً منسوخ اعلام شده است.
    /// </summary>
    [Obsolete("جایگزین شده با کاتالوگ جایگاه (BannerSlot.DailyPrice × DurationDays => TotalAmount). پس از بازنویسی جریان پرداخت در تسک‌های بعدی حذف خواهد شد.")]
    public const long FeeAmountInRials = 2_000_000L;

    /// <summary>مدت زمان پیش‌فرض نمایش (روز) — قابل دسترسی از لایه Application برای مقداردهی پیش‌فرض DurationDays.</summary>
    public const int DefaultDurationDays = 30;

    public Guid CompanyId { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public string DestinationUrl { get; private set; } = string.Empty;
    public BannerPlacement Placement { get; private set; }
    public BannerAdStatus Status { get; private set; }
    public bool IsFeePaid { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? SubmittedForReviewAtUtc { get; private set; }
    public DateTime? ActivatedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }

    // --- فاز «مدیریت و رزرو بنرهای تبلیغاتی» (کاتالوگ جایگاه + قیمت‌گذاری روزانه + آمار اولیه) ---

    /// <summary>جایگاه انتخابی از کاتالوگ BannerSlot — Nullable تا اتصال کامل در تسک‌های بعدی انجام شود.</summary>
    public int? BannerSlotId { get; private set; }
    public BannerSlot? BannerSlot { get; private set; }

    /// <summary>مدت زمان نمایش (روز) — پیش‌فرض ۳۰ روز.</summary>
    public int DurationDays { get; private set; } = DefaultDurationDays;

    /// <summary>تاریخ واقعی شروع نمایش — تا زمان تایید ادمین نامشخص است (معادل ActivatedAtUtc پس از Approve).</summary>
    public DateTime? StartDate { get; private set; }

    /// <summary>تاریخ واقعی پایان نمایش — تا زمان تایید ادمین نامشخص است (معادل ExpiresAtUtc پس از Approve).</summary>
    public DateTime? EndDate { get; private set; }

    /// <summary>مبلغ قطعی فاکتور (BannerSlot.DailyPrice × DurationDays در لحظه رزرو).</summary>
    public decimal TotalAmount { get; private set; }

    public long ImpressionsCount { get; private set; }
    public long ClicksCount { get; private set; }

    private BannerAd() { }

    private BannerAd(Guid id, Guid companyId, string imageUrl, string destinationUrl, BannerPlacement placement) : base(id)
    {
        CompanyId = companyId;
        ImageUrl = imageUrl;
        DestinationUrl = destinationUrl;
        Placement = placement;
        Status = BannerAdStatus.Draft;
        IsFeePaid = false;
        DurationDays = DefaultDurationDays;
        ImpressionsCount = 0;
        ClicksCount = 0;
    }

    public static Result<BannerAd> Create(Guid companyId, string imageUrl, string destinationUrl, BannerPlacement placement)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return Result.Failure<BannerAd>(Error.Validation("VALIDATION_ERROR", "تصویر بنر الزامی است."));

        if (string.IsNullOrWhiteSpace(destinationUrl))
            return Result.Failure<BannerAd>(Error.Validation("VALIDATION_ERROR", "لینک مقصد بنر الزامی است."));

        return Result.Success(new BannerAd(Guid.NewGuid(), companyId, imageUrl, destinationUrl, placement));
    }

    public Result Update(string imageUrl, string destinationUrl, BannerPlacement placement)
    {
        if (Status is not (BannerAdStatus.Draft or BannerAdStatus.Rejected))
            return Result.Failure(Error.Conflict("BANNER_AD_NOT_EDITABLE", "این بنر در وضعیتی نیست که قابل ویرایش باشد."));

        if (string.IsNullOrWhiteSpace(imageUrl))
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "تصویر بنر الزامی است."));

        if (string.IsNullOrWhiteSpace(destinationUrl))
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "لینک مقصد بنر الزامی است."));

        ImageUrl = imageUrl;
        DestinationUrl = destinationUrl;
        Placement = placement;
        return Result.Success();
    }

    /// <summary>
    /// انتخاب جایگاه از کاتالوگ و قفل‌کردن قیمت روزانه لحظه رزرو — طبق فاز «مدیریت و رزرو بنرهای
    /// تبلیغاتی». فراخوانی آن به CreateBannerAdCommand بازنویسی‌شده در تسک‌های بعدی این فاز موکول شده.
    /// </summary>
    public Result AssignSlotAndPricing(int bannerSlotId, decimal dailyPrice, int durationDays = DefaultDurationDays)
    {
        if (Status is not (BannerAdStatus.Draft or BannerAdStatus.Rejected))
            return Result.Failure(Error.Conflict("BANNER_AD_NOT_EDITABLE", "این بنر در وضعیتی نیست که جایگاه آن قابل تغییر باشد."));

        if (bannerSlotId <= 0)
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "انتخاب جایگاه بنر الزامی است."));

        if (dailyPrice <= 0)
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "قیمت روزانه جایگاه نامعتبر است."));

        if (durationDays <= 0)
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "مدت زمان نمایش باید بزرگ‌تر از صفر باشد."));

        BannerSlotId = bannerSlotId;
        DurationDays = durationDays;
        TotalAmount = dailyPrice * durationDays;
        return Result.Success();
    }

    /// <summary>فراخوانی پس از تایید موفق پرداخت هزینه ثابت بنر (سند Payments، طبق ADR-008).</summary>
    public Result MarkFeePaid()
    {
        if (Status is not (BannerAdStatus.Draft or BannerAdStatus.Rejected))
            return Result.Failure(Error.Conflict("BANNER_AD_NOT_PAYABLE", "پرداخت هزینه فقط برای بنرهای پیش‌نویس یا ردشده امکان‌پذیر است."));

        IsFeePaid = true;
        return Result.Success();
    }

    public Result SubmitForReview(DateTime utcNow)
    {
        if (Status is not (BannerAdStatus.Draft or BannerAdStatus.Rejected))
            return Result.Failure(Error.Conflict("BANNER_AD_NOT_SUBMITTABLE", "این بنر در وضعیتی نیست که قابل ارسال برای بررسی باشد."));

        if (!IsFeePaid)
            return Result.Failure(Error.Conflict("BANNER_AD_FEE_NOT_PAID", "ابتدا باید هزینه بنر پرداخت شود."));

        Status = BannerAdStatus.PendingReview;
        SubmittedForReviewAtUtc = utcNow;
        RejectionReason = null;
        return Result.Success();
    }

    public Result Approve(DateTime utcNow)
    {
        if (Status is not BannerAdStatus.PendingReview)
            return Result.Failure(Error.Conflict("BANNER_AD_NOT_PENDING", "فقط بنرهای در انتظار بررسی قابل تایید هستند."));

        Status = BannerAdStatus.Active;
        ActivatedAtUtc = utcNow;
        StartDate = utcNow;
        ExpiresAtUtc = utcNow.AddDays(DurationDays);
        EndDate = ExpiresAtUtc;
        RejectionReason = null;
        return Result.Success();
    }

    public Result Reject(string reason)
    {
        if (Status is not BannerAdStatus.PendingReview)
            return Result.Failure(Error.Conflict("BANNER_AD_NOT_PENDING", "فقط بنرهای در انتظار بررسی قابل رد هستند."));

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "ذکر دلیل رد بنر الزامی است."));

        Status = BannerAdStatus.Rejected;
        RejectionReason = reason;
        return Result.Success();
    }

    /// <summary>فراخوانی توسط Background Job برای انقضای خودکار — طبق ADR-008.</summary>
    public void MarkExpiredIfNeeded(DateTime utcNow)
    {
        if (Status == BannerAdStatus.Active && ExpiresAtUtc.HasValue && utcNow >= ExpiresAtUtc.Value)
        {
            Status = BannerAdStatus.Expired;
        }
    }

    /// <summary>
    /// تمدید نمایش بنر با تخفیف — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۵).
    /// فقط برای بنرهای Active یا Expired مجاز است. اگر بنر هنوز Active و در حال نمایش باشد،
    /// تاریخ پایان از EndDate فعلی جمع می‌شود (بدون قطع نمایش)؛ اگر Expired باشد، بازه نمایش از اکنون
    /// از نو آغاز می‌شود و وضعیت به Active بازمی‌گردد. بررسی تداخل جایگاه برای حالت Expired پیش از
    /// فراخوانی این متد، در لایه Application انجام می‌شود (عیناً الگوی Approve/تسک ۴).
    /// </summary>
    public Result Renew(int additionalDays, decimal renewalAmount, DateTime utcNow)
    {
        if (Status is not (BannerAdStatus.Active or BannerAdStatus.Expired))
            return Result.Failure(Error.Conflict("BANNER_AD_NOT_RENEWABLE", "فقط بنرهای فعال یا منقضی‌شده قابل تمدید هستند."));

        if (additionalDays <= 0)
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "مدت زمان تمدید باید بزرگ‌تر از صفر باشد."));

        if (renewalAmount <= 0)
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "مبلغ تمدید نامعتبر است."));

        if (Status == BannerAdStatus.Active && EndDate.HasValue && EndDate.Value > utcNow)
        {
            EndDate = EndDate.Value.AddDays(additionalDays);
        }
        else
        {
            StartDate = utcNow;
            EndDate = utcNow.AddDays(additionalDays);
            ActivatedAtUtc = utcNow;
            Status = BannerAdStatus.Active;
        }

        ExpiresAtUtc = EndDate;
        DurationDays += additionalDays;
        TotalAmount += renewalAmount;
        RejectionReason = null;

        return Result.Success();
    }

    /// <summary>ثبت یک بازدید — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (شمارنده سبک، بدون جدول Event مجزا در این تسک).</summary>
    public void RecordImpression() => ImpressionsCount++;

    /// <summary>ثبت یک کلیک — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی».</summary>
    public void RecordClick() => ClicksCount++;

    public bool IsDisplayable() => Status is BannerAdStatus.Active;
}
