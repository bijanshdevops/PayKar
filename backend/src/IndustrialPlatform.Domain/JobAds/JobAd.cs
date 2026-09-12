using IndustrialPlatform.Shared.Entities;
using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Domain.JobAds;

/// <summary>
/// آگهی استخدامی — Aggregate Root طبق سند 02-Domain-Glossary.md بخش ۳.
/// مدت اعتبار پیش‌فرض انتشار: ۳۰ روز (سند 02، بخش ۳.۱).
/// جریان انتشار (ADR جدید «تایید ادمین + هزینه ثابت»، جایگزین کامل سیستم ارتقاء/Boost):
/// Draft → پرداخت هزینه ثابت (IsFeePaid=true) → SubmitForReview → PendingReview
///       → تایید ادمین (Approve) → Published
///       → رد ادمین (Reject) → Rejected → اصلاح (Update) → ارسال مجدد (SubmitForReview، بدون پرداخت مجدد) → PendingReview
/// </summary>
public sealed class JobAd : AggregateRoot<Guid>
{
    /// <summary>هزینه ثابت ثبت آگهی — ۵۰۰٬۰۰۰ ریال (۵۰,۰۰۰ تومان)، طبق مستندات مرجع فاز جدید.</summary>
    public const long ListingFeeAmountInRials = 500_000L;
    private const int DefaultPublishDurationDays = 30;

    public Guid CompanyId { get; private set; }
    public Guid IndustrialZoneId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public WorkShift WorkShift { get; private set; }
    public bool HasCommuteService { get; private set; }
    public string? CommuteServiceRoutes { get; private set; }
    public MealPlan MealPlan { get; private set; }
    public InsuranceType InsuranceTypes { get; private set; }
    public SalaryRange SalaryRange { get; private set; } = null!;

    // ---------- فیلدهای تکمیلی استخدام (الزامی: ContractType — مابقی اختیاری) ----------
    public ContractType ContractType { get; private set; }
    public GenderPreference GenderPreference { get; private set; } = GenderPreference.Any;
    public int? MinAge { get; private set; }
    public int? MaxAge { get; private set; }
    public EducationLevel MinEducationLevel { get; private set; } = EducationLevel.Unspecified;
    public int? MinExperienceYears { get; private set; }
    public MilitaryServiceStatus? MilitaryServiceStatus { get; private set; }
    public int? HeadcountNeeded { get; private set; }
    public DateTime? ApplicationDeadlineUtc { get; private set; }
    public string? RequiredSkills { get; private set; }
    public string? AdditionalBenefits { get; private set; }

    // ---------- جریان تایید ادمین و پرداخت ----------
    public JobAdStatus Status { get; private set; }
    public bool IsFeePaid { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? SubmittedForReviewAtUtc { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }

    /// <summary>
    /// تعداد بازدید صفحه جزئیات آگهی — عدد واقعی و انباشتی، از طریق <see cref="IncrementViewsCount"/>
    /// (فراخوانی‌شده توسط یک Command مجزا، نه به‌صورت side-effect در Query خواندن).
    /// </summary>
    public int ViewsCount { get; private set; }

    /// <summary>
    /// فلگ ساده «ویژه» برای نمایش در کاروسل صفحه اصلی — فقط توسط ادمین قابل تغییر است (نه پلن پرداختی/Boost).
    /// جایگزین AdBoostPlan حذف‌شده (تسک #38) با یک سوییچ ساده و بدون هزینه.
    /// </summary>
    public bool IsFeatured { get; private set; }

    private JobAd() { }

    private JobAd(
        Guid id,
        Guid companyId,
        Guid industrialZoneId,
        string title,
        string description,
        WorkShift workShift,
        bool hasCommuteService,
        string? commuteServiceRoutes,
        MealPlan mealPlan,
        InsuranceType insuranceTypes,
        SalaryRange salaryRange,
        ContractType contractType,
        GenderPreference genderPreference,
        int? minAge,
        int? maxAge,
        EducationLevel minEducationLevel,
        int? minExperienceYears,
        MilitaryServiceStatus? militaryServiceStatus,
        int? headcountNeeded,
        DateTime? applicationDeadlineUtc,
        string? requiredSkills,
        string? additionalBenefits) : base(id)
    {
        CompanyId = companyId;
        IndustrialZoneId = industrialZoneId;
        Title = title;
        Description = description;
        WorkShift = workShift;
        HasCommuteService = hasCommuteService;
        CommuteServiceRoutes = commuteServiceRoutes;
        MealPlan = mealPlan;
        InsuranceTypes = insuranceTypes;
        SalaryRange = salaryRange;
        ContractType = contractType;
        GenderPreference = genderPreference;
        MinAge = minAge;
        MaxAge = maxAge;
        MinEducationLevel = minEducationLevel;
        MinExperienceYears = minExperienceYears;
        MilitaryServiceStatus = militaryServiceStatus;
        HeadcountNeeded = headcountNeeded;
        ApplicationDeadlineUtc = applicationDeadlineUtc;
        RequiredSkills = requiredSkills;
        AdditionalBenefits = additionalBenefits;
        Status = JobAdStatus.Draft;
        IsFeePaid = false;
        ViewsCount = 0;
        IsFeatured = false;
    }

    public static Result<JobAd> Create(
        Guid companyId,
        Guid industrialZoneId,
        string title,
        string description,
        WorkShift workShift,
        bool hasCommuteService,
        string? commuteServiceRoutes,
        MealPlan mealPlan,
        InsuranceType insuranceTypes,
        SalaryRange salaryRange,
        ContractType contractType,
        GenderPreference genderPreference = GenderPreference.Any,
        int? minAge = null,
        int? maxAge = null,
        EducationLevel minEducationLevel = EducationLevel.Unspecified,
        int? minExperienceYears = null,
        MilitaryServiceStatus? militaryServiceStatus = null,
        int? headcountNeeded = null,
        DateTime? applicationDeadlineUtc = null,
        string? requiredSkills = null,
        string? additionalBenefits = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<JobAd>(Error.Validation("VALIDATION_ERROR", "عنوان آگهی الزامی است."));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<JobAd>(Error.Validation("VALIDATION_ERROR", "شرح آگهی الزامی است."));

        if (minAge.HasValue && maxAge.HasValue && minAge > maxAge)
            return Result.Failure<JobAd>(Error.Validation("VALIDATION_ERROR", "حداقل سن نمی‌تواند بیشتر از حداکثر سن باشد."));

        if (headcountNeeded.HasValue && headcountNeeded.Value <= 0)
            return Result.Failure<JobAd>(Error.Validation("VALIDATION_ERROR", "تعداد نیروی موردنیاز باید بزرگ‌تر از صفر باشد."));

        return Result.Success(new JobAd(
            Guid.NewGuid(), companyId, industrialZoneId, title, description,
            workShift, hasCommuteService, commuteServiceRoutes, mealPlan, insuranceTypes, salaryRange,
            contractType, genderPreference, minAge, maxAge, minEducationLevel, minExperienceYears,
            militaryServiceStatus, headcountNeeded, applicationDeadlineUtc, requiredSkills, additionalBenefits));
    }

    public Result Update(
        string title,
        string description,
        WorkShift workShift,
        bool hasCommuteService,
        string? commuteServiceRoutes,
        MealPlan mealPlan,
        InsuranceType insuranceTypes,
        SalaryRange salaryRange,
        ContractType contractType,
        GenderPreference genderPreference,
        int? minAge,
        int? maxAge,
        EducationLevel minEducationLevel,
        int? minExperienceYears,
        MilitaryServiceStatus? militaryServiceStatus,
        int? headcountNeeded,
        DateTime? applicationDeadlineUtc,
        string? requiredSkills,
        string? additionalBenefits)
    {
        if (Status is JobAdStatus.PendingReview or JobAdStatus.Closed or JobAdStatus.Archived)
            return Result.Failure(Error.Conflict("JOB_AD_NOT_EDITABLE", "این آگهی در وضعیتی نیست که قابل ویرایش باشد."));

        if (minAge.HasValue && maxAge.HasValue && minAge > maxAge)
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "حداقل سن نمی‌تواند بیشتر از حداکثر سن باشد."));

        Title = title;
        Description = description;
        WorkShift = workShift;
        HasCommuteService = hasCommuteService;
        CommuteServiceRoutes = commuteServiceRoutes;
        MealPlan = mealPlan;
        InsuranceTypes = insuranceTypes;
        SalaryRange = salaryRange;
        ContractType = contractType;
        GenderPreference = genderPreference;
        MinAge = minAge;
        MaxAge = maxAge;
        MinEducationLevel = minEducationLevel;
        MinExperienceYears = minExperienceYears;
        MilitaryServiceStatus = militaryServiceStatus;
        HeadcountNeeded = headcountNeeded;
        ApplicationDeadlineUtc = applicationDeadlineUtc;
        RequiredSkills = requiredSkills;
        AdditionalBenefits = additionalBenefits;

        return Result.Success();
    }

    /// <summary>فراخوانی پس از تایید موفق پرداخت هزینه ثابت ثبت آگهی (سند جدید Payments).</summary>
    public Result MarkFeePaid()
    {
        if (Status is not (JobAdStatus.Draft or JobAdStatus.Rejected))
            return Result.Failure(Error.Conflict("JOB_AD_NOT_PAYABLE", "پرداخت هزینه فقط برای آگهی‌های پیش‌نویس یا ردشده امکان‌پذیر است."));

        IsFeePaid = true;
        return Result.Success();
    }

    /// <summary>
    /// ارسال آگهی به صف بررسی ادمین. نیازمند پرداخت قبلی هزینه ثابت است.
    /// در صورت ارسال مجدد پس از رد (Rejected)، چون هزینه پیش‌تر پرداخت شده، نیازی به پرداخت دوباره نیست.
    /// </summary>
    public Result SubmitForReview(DateTime utcNow)
    {
        if (Status is not (JobAdStatus.Draft or JobAdStatus.Rejected))
            return Result.Failure(Error.Conflict("JOB_AD_NOT_SUBMITTABLE", "این آگهی در وضعیتی نیست که قابل ارسال برای بررسی باشد."));

        if (!IsFeePaid)
            return Result.Failure(Error.Conflict("JOB_AD_FEE_NOT_PAID", "ابتدا باید هزینه ثبت آگهی پرداخت شود."));

        Status = JobAdStatus.PendingReview;
        SubmittedForReviewAtUtc = utcNow;
        RejectionReason = null;
        return Result.Success();
    }

    /// <summary>تایید آگهی توسط ادمین/استعلام — طبق درخواست کاربر برای پنل Owner.</summary>
    public Result Approve(DateTime utcNow)
    {
        if (Status is not JobAdStatus.PendingReview)
            return Result.Failure(Error.Conflict("JOB_AD_NOT_PENDING", "فقط آگهی‌های در انتظار بررسی قابل تایید هستند."));

        Status = JobAdStatus.Published;
        PublishedAtUtc = utcNow;
        ExpiresAtUtc = utcNow.AddDays(DefaultPublishDurationDays);
        RejectionReason = null;
        return Result.Success();
    }

    /// <summary>رد آگهی توسط ادمین با ذکر دلیل — شرکت می‌تواند اصلاح و بدون پرداخت مجدد ارسال کند.</summary>
    public Result Reject(string reason)
    {
        if (Status is not JobAdStatus.PendingReview)
            return Result.Failure(Error.Conflict("JOB_AD_NOT_PENDING", "فقط آگهی‌های در انتظار بررسی قابل رد هستند."));

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "ذکر دلیل رد آگهی الزامی است."));

        Status = JobAdStatus.Rejected;
        RejectionReason = reason;
        return Result.Success();
    }

    public Result Close()
    {
        if (Status is not JobAdStatus.Published)
            return Result.Failure(Error.Conflict("JOB_AD_NOT_PUBLISHABLE", "فقط آگهی‌های منتشرشده قابل بستن هستند."));

        Status = JobAdStatus.Closed;
        return Result.Success();
    }

    public Result Archive()
    {
        Status = JobAdStatus.Archived;
        return Result.Success();
    }

    /// <summary>
    /// بررسی امکان حذف منطقی — طبق فاز بازطراحی «آگهی‌های شرکت من»: آگهی‌های در حال بررسی ادمین
    /// یا منتشرشده قابل حذف نیستند (برای منتشرشده، ابتدا باید <see cref="Close"/> فراخوانی شود؛
    /// برای در حال بررسی، حذف در میانهٔ استعلام ادمین مجاز نیست). سایر وضعیت‌ها (پیش‌نویس، ردشده،
    /// منقضی‌شده، بسته‌شده، آرشیوشده) قابل حذف‌اند.
    /// </summary>
    public Result EnsureDeletable()
    {
        if (Status is JobAdStatus.PendingReview or JobAdStatus.Published)
        {
            var reason = Status == JobAdStatus.Published
                ? "آگهی منتشرشده را نمی‌توان حذف کرد؛ ابتدا آن را ببندید."
                : "آگهی در حال بررسی ادمین را نمی‌توان حذف کرد.";
            return Result.Failure(Error.Conflict("JOB_AD_NOT_DELETABLE", reason));
        }

        return Result.Success();
    }

    /// <summary>فراخوانی توسط Background Job برای انقضای خودکار (سند 07-Roadmap.md فاز ۳).</summary>
    public void MarkExpiredIfNeeded(DateTime utcNow)
    {
        if (Status == JobAdStatus.Published && ExpiresAtUtc.HasValue && utcNow >= ExpiresAtUtc.Value)
        {
            Status = JobAdStatus.Expired;
        }
    }

    public bool IsPublishable() => Status is JobAdStatus.Published;

    /// <summary>ثبت یک بازدید واقعی از صفحه جزئیات آگهی. فراخوانی‌شده از RecordJobAdViewCommand.</summary>
    public void IncrementViewsCount() => ViewsCount++;

    /// <summary>
    /// تغییر فلگ «ویژه» توسط ادمین. یک سوییچ نمایشی ساده برای کاروسل صفحه اصلی —
    /// بدون وابستگی به هیچ پلن پرداختی (طبق تصمیم صریح: عدم بازگشت به سیستم AdBoostPlan حذف‌شده).
    /// مجوز Admin در لایه Application/Api کنترل می‌شود، نه در Domain.
    /// </summary>
    public void SetFeatured(bool isFeatured) => IsFeatured = isFeatured;
}
