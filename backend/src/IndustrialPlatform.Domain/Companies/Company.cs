using IndustrialPlatform.Shared.Entities;
using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Domain.Companies;

/// <summary>
/// شرکت/کارخانه مستقر در شهرک صنعتی — Aggregate Root طبق سند 02-Domain-Glossary.md بخش ۲.۱.
/// یادداشت معماری: OwnerUserId صرفاً یک شناسه Guid است (بدون FK/ProjectReference به ماژول Identity)
/// چون طبق ماتریس وابستگی سند 01-Architecture، Domain/Application اجازه ارجاع به Identity را ندارند.
/// </summary>
public sealed class Company : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string NationalId { get; private set; } = string.Empty;
    public string RegistrationNumber { get; private set; } = string.Empty;
    public Guid IndustrialZoneId { get; private set; }
    public string AddressDetail { get; private set; } = string.Empty;
    public string IndustryCategory { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public VerificationStatus VerificationStatus { get; private set; }
    public Guid OwnerUserId { get; private set; }

    /// <summary>
    /// شماره تماس عمومی شرکت — طبق ADR-011 برای دکمه «تماس با کارفرما» در صفحه جزئیات آگهی نمایش داده می‌شود.
    /// Nullable است تا شرکت‌های ثبت‌شده پیش از این تصمیم محصولی نشکنند؛ از طریق ویرایش پروفایل قابل تکمیل است.
    /// </summary>
    public string? ContactPhoneNumber { get; private set; }

    /// <summary>تصویر بنر/کاور بالای کارت پروفایل شرکت — طبق فاز بازطراحی صفحه پروفایل. اختیاری، مشابه LogoUrl.</summary>
    public string? BannerUrl { get; private set; }

    /// <summary>وب‌سایت رسمی شرکت — اختیاری، صرفاً برای نمایش در پروفایل عمومی.</summary>
    public string? Website { get; private set; }

    /// <summary>ایمیل سازمانی شرکت — اختیاری، مستقل از ایمیل حساب کاربری مالک.</summary>
    public string? Email { get; private set; }

    /// <summary>معرفی کوتاه/درباره شرکت — اختیاری، متن آزاد برای نمایش در پروفایل.</summary>
    public string? Description { get; private set; }

    /// <summary>
    /// موجودی کیف‌پول شرکت. طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۵)، اکنون واقعاً توسط
    /// <see cref="DebitWallet"/> در جریان تمدید بنر با تخفیف کسر می‌شود؛ برای سایر جریان‌های پرداخت
    /// (ثبت آگهی/رزومه/بنر جدید) هنوز مدل مستقیم زرین‌پال حاکم است و این فیلد دست‌نخورده می‌ماند.
    /// شارژ کیف‌پول (Top-up) هنوز پیاده‌سازی نشده — طبق افشای صریح در صفحه «امور مالی و تراکنش‌ها».
    /// </summary>
    public long WalletBalanceInRials { get; private set; }

    /// <summary>
    /// کسر مبلغ از موجودی کیف‌پول — طبق فاز «مدیریت و رزرو بنرهای تبلیغاتی» (تسک ۵).
    /// در صورت ناکافی بودن موجودی، Result ناموفق با پیام بیزینسی خوانا برمی‌گرداند.
    /// </summary>
    public Result DebitWallet(long amountInRials)
    {
        if (amountInRials <= 0)
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "مبلغ کسر از کیف پول باید بزرگ‌تر از صفر باشد."));

        if (WalletBalanceInRials < amountInRials)
            return Result.Failure(Error.Conflict("INSUFFICIENT_WALLET_BALANCE", "موجودی کیف پول شرکت برای این تراکنش کافی نیست."));

        WalletBalanceInRials -= amountInRials;
        return Result.Success();
    }

    private Company() { }

    private Company(
        Guid id,
        string name,
        string nationalId,
        string registrationNumber,
        Guid industrialZoneId,
        string addressDetail,
        string industryCategory,
        Guid ownerUserId,
        string? contactPhoneNumber,
        string? website,
        string? email,
        string? description) : base(id)
    {
        Name = name;
        NationalId = nationalId;
        RegistrationNumber = registrationNumber;
        IndustrialZoneId = industrialZoneId;
        AddressDetail = addressDetail;
        IndustryCategory = industryCategory;
        OwnerUserId = ownerUserId;
        ContactPhoneNumber = contactPhoneNumber;
        Website = website;
        Email = email;
        Description = description;
        VerificationStatus = VerificationStatus.PendingVerification;
        WalletBalanceInRials = 0;
    }

    public static Company Create(
        string name,
        string nationalId,
        string registrationNumber,
        Guid industrialZoneId,
        string addressDetail,
        string industryCategory,
        Guid ownerUserId,
        string? contactPhoneNumber = null,
        string? website = null,
        string? email = null,
        string? description = null) => new(
            Guid.NewGuid(), name, nationalId, registrationNumber, industrialZoneId, addressDetail, industryCategory, ownerUserId,
            contactPhoneNumber, website, email, description);

    public Result Update(
        string name,
        string addressDetail,
        string industryCategory,
        Guid industrialZoneId,
        string? contactPhoneNumber = null,
        string? website = null,
        string? email = null,
        string? description = null)
    {
        Name = name;
        AddressDetail = addressDetail;
        IndustryCategory = industryCategory;
        IndustrialZoneId = industrialZoneId;
        if (contactPhoneNumber is not null)
            ContactPhoneNumber = contactPhoneNumber;
        // طبق الگوی ContactPhoneNumber: null یعنی «تغییری داده نشده»، رشتهٔ خالی یعنی «عمداً پاک شد».
        if (website is not null)
            Website = string.IsNullOrWhiteSpace(website) ? null : website;
        if (email is not null)
            Email = string.IsNullOrWhiteSpace(email) ? null : email;
        if (description is not null)
            Description = string.IsNullOrWhiteSpace(description) ? null : description;
        return Result.Success();
    }

    public void SetLogo(string logoUrl) => LogoUrl = logoUrl;

    public void SetBannerUrl(string bannerUrl) => BannerUrl = bannerUrl;

    public Result RequestVerification()
    {
        if (VerificationStatus is VerificationStatus.Verified)
            return Result.Failure(Error.Conflict("COMPANY_ALREADY_VERIFIED", "این شرکت پیش‌تر تایید شده است."));

        VerificationStatus = VerificationStatus.PendingVerification;
        return Result.Success();
    }

    public Result Approve()
    {
        VerificationStatus = VerificationStatus.Verified;
        return Result.Success();
    }

    public Result Reject()
    {
        VerificationStatus = VerificationStatus.Rejected;
        return Result.Success();
    }

    public Result Suspend()
    {
        VerificationStatus = VerificationStatus.Suspended;
        return Result.Success();
    }

    public bool IsOwnedBy(Guid userId) => OwnerUserId == userId;
}
