using IndustrialPlatform.Shared.Entities;
using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Domain.Payments;

public enum PaymentTransactionStatus
{
    Pending = 1,
    Success = 2,
    Failed = 3
}

/// <summary>
/// هدف تراکنش پرداخت. طبق تصمیم محصولی جدید (جایگزینی کامل سیستم پلن‌های ارتقاء/Boost)
/// هدف اول هزینه ثابت ثبت آگهی بود؛ طبق ADR-008 هدف دوم (هزینه بنر تبلیغاتی در پنل Owner)
/// و طبق ADR-013 هدف سوم (هزینه رزومه‌ساز کارجو) نیز اکنون فعال شده‌اند.
/// </summary>
public enum PaymentPurpose
{
    JobAdListingFee = 1,
    BannerAdFee = 2,
    ResumeCreationFee = 3,

    /// <summary>تمدید بنر تبلیغاتی با ۲۰٪ تخفیف از طریق کسر کیف‌پول — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۵).</summary>
    BannerAdRenewal = 4
}

/// <summary>
/// تراکنش پرداخت — طبق سند 05-Security-Rules.md بخش ۶:
/// مبلغ نهایی همیشه از این رکورد خوانده می‌شود، نه از ورودی کلاینت.
/// طبق ADR-008: JobAdId/BannerAdId هر دو Nullable هستند و بسته به Purpose دقیقاً یکی از آن‌ها مقدار دارد.
/// طبق ADR-013: پرداخت‌کننده می‌تواند شرکت (CompanyId) یا کارجو (CandidateId) باشد — دقیقاً یکی از این دو
/// بسته به Purpose مقدار دارد؛ CompanyId به همین دلیل Nullable شده است.
/// </summary>
public sealed class PaymentTransaction : AggregateRoot<Guid>
{
    public Guid? CompanyId { get; private set; }
    public Guid? CandidateId { get; private set; }
    public Guid? JobAdId { get; private set; }
    public Guid? BannerAdId { get; private set; }
    public PaymentPurpose Purpose { get; private set; }
    public long AmountInRials { get; private set; }
    public string Authority { get; private set; } = string.Empty;
    public PaymentTransactionStatus Status { get; private set; }
    public string? RefId { get; private set; }

    /// <summary>
    /// شرح آزاد تراکنش — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۵)، فقط برای تراکنش‌های
    /// کسر از کیف‌پول (مثل تمدید بنر) پر می‌شود؛ برای تراکنش‌های زرین‌پال قبلی Nullable و خالی می‌ماند.
    /// </summary>
    public string? Description { get; private set; }

    private PaymentTransaction() { }

    private PaymentTransaction(
        Guid id, Guid? companyId, Guid? candidateId, Guid? jobAdId, Guid? bannerAdId,
        PaymentPurpose purpose, long amountInRials, string authority, string? description = null) : base(id)
    {
        CompanyId = companyId;
        CandidateId = candidateId;
        JobAdId = jobAdId;
        BannerAdId = bannerAdId;
        Purpose = purpose;
        AmountInRials = amountInRials;
        Authority = authority;
        Description = description;
        Status = PaymentTransactionStatus.Pending;
    }

    public static PaymentTransaction Create(Guid companyId, Guid jobAdId, long amountInRials, string authority, PaymentPurpose purpose = PaymentPurpose.JobAdListingFee) =>
        new(Guid.NewGuid(), companyId, null, jobAdId, null, purpose, amountInRials, authority);

    /// <summary>طبق ADR-008: ساخت تراکنش برای هزینه بنر تبلیغاتی.</summary>
    public static PaymentTransaction CreateForBannerAd(Guid companyId, Guid bannerAdId, long amountInRials, string authority) =>
        new(Guid.NewGuid(), companyId, null, null, bannerAdId, PaymentPurpose.BannerAdFee, amountInRials, authority);

    /// <summary>طبق ADR-013: ساخت تراکنش برای هزینه رزومه‌ساز — پرداخت‌کننده کارجو است، نه شرکت.</summary>
    public static PaymentTransaction CreateForResumeFee(Guid candidateId, long amountInRials, string authority) =>
        new(Guid.NewGuid(), null, candidateId, null, null, PaymentPurpose.ResumeCreationFee, amountInRials, authority);

    /// <summary>
    /// ساخت و علامت‌گذاری فوری یک تراکنش موفق برای کسر مستقیم از کیف‌پول — طبق فاز «مدیریت و رزرو
    /// بنرهای تبلیغاتی» (تسک ۵). برخلاف تراکنش‌های زرین‌پال، این تراکنش هیچ Authority/Callback واقعی
    /// از درگاه ندارد (پرداخت بلافاصله و داخلی است)، پس یک Authority مصنوعی و یکتا برای آن تولید می‌شود.
    /// </summary>
    public static PaymentTransaction CreateSucceededForWalletDebit(Guid companyId, Guid bannerAdId, long amountInRials, PaymentPurpose purpose, string description)
    {
        var authority = $"WALLET-{Guid.NewGuid():N}";
        var transaction = new PaymentTransaction(Guid.NewGuid(), companyId, null, null, bannerAdId, purpose, amountInRials, authority, description);
        transaction.MarkSuccess(authority);
        return transaction;
    }

    public Result MarkSuccess(string refId)
    {
        if (Status != PaymentTransactionStatus.Pending)
            return Result.Failure(Error.Conflict("PAYMENT_ALREADY_PROCESSED", "این تراکنش قبلاً پردازش شده است."));

        Status = PaymentTransactionStatus.Success;
        RefId = refId;
        return Result.Success();
    }

    public Result MarkFailed()
    {
        if (Status != PaymentTransactionStatus.Pending)
            return Result.Failure(Error.Conflict("PAYMENT_ALREADY_PROCESSED", "این تراکنش قبلاً پردازش شده است."));

        Status = PaymentTransactionStatus.Failed;
        return Result.Success();
    }
}
