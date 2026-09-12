using IndustrialPlatform.Shared.Entities;
using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Domain.Candidates;

/// <summary>
/// کارجو و رزومه او — Aggregate Root طبق سند 02-Domain-Glossary.md بخش ۴.۱
/// (نگاشت CandidateResume در جدول اصطلاحات). به ازای هر کاربر با نقش Candidate یک نمونه.
/// یادداشت: MobileNumber به‌صورت غیرنرمال از Identity کپی می‌شود (طبق ماتریس وابستگی، Domain/Application
/// اجازه ارجاع مستقیم به ماژول Identity را ندارند) تا امکان اطلاع‌رسانی پیامکی وضعیت درخواست فراهم شود.
/// </summary>
public sealed class Candidate : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string MobileNumber { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public MilitaryServiceStatus MilitaryServiceStatus { get; private set; }
    public string EducationLevel { get; private set; } = string.Empty;
    public string? WorkExperienceSummary { get; private set; }
    public string? Skills { get; private set; }

    /// <summary>علاقه‌مندی‌ها — مرحله پنجم رزومه‌ساز مرحله‌ای.</summary>
    public string? Interests { get; private set; }

    /// <summary>
    /// ایمیل تماس کارجو — طبق تصمیم صریح محصولی فاز «مدیریت رزومه‌ها و متقاضیان»: برخلاف MobileNumber
    /// (که از Identity کپی می‌شود)، این فیلد کاملاً اختیاری و مستقیماً توسط خود کارجو در رزومه‌ساز وارد
    /// می‌شود؛ ممکن است null باشد (فرانت باید این حالت را به‌جای مقدار جعلی به‌درستی مدیریت کند).
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>شهر محل سکونت — متن آزاد (هم‌راستا با الگوی Company.AddressDetail)، اختیاری.</summary>
    public string? City { get; private set; }

    /// <summary>
    /// پاسخ‌های چند سؤال روان‌شناسی عمومی رزومه‌ساز — به‌صورت متن آزاد/JSON ساده ذخیره می‌شود
    /// (مشابه الگوی Skills/WorkExperienceSummary؛ بدون نیاز به مدل‌سازی جداگانه در این فاز).
    /// </summary>
    public string? PsychologyAnswers { get; private set; }

    /// <summary>مسیر فایل رزومه آپلودی — برای مسیر «ارسال مستقیم بدون رزومه‌ساز».</summary>
    public string? ResumeFileUrl { get; private set; }

    /// <summary>
    /// تصویر آواتار کارجو — طبق تصمیم محصولی جدید، هنگام Apply روی یک آگهی مشخص در اختیار همان
    /// کارفرما قرار می‌گیرد (به همراه نام/سوابق/رزومه). آپلود از طریق UploadCandidateAvatarCommand.
    /// </summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// طبق ADR-013: آیا هزینه ساخت رزومه (۲۵٬۰۰۰ تومان) برای این پروفایل پرداخت شده است.
    /// برای مسیر «ارسال مستقیم» (<see cref="CreateFromDirectUpload"/>) این هزینه از ابتدا اعمال نمی‌شود
    /// و مقدار همواره true است. ویرایش‌های بعدی رزومه‌ای که یک‌بار پرداخت شده، رایگان است.
    /// </summary>
    public bool IsFeePaid { get; private set; }

    /// <summary>مبلغ ثابت هزینه ساخت رزومه جدید — طبق ADR-013 (۲۵۰,۰۰۰ ریال = ۲۵,۰۰۰ تومان).</summary>
    public const long ResumeCreationFeeAmountInRials = 250_000;

    // ---------- فیلدهای فاز «پروفایل و رزومه‌ساز کارجو» ----------

    /// <summary>عنوان شغلی/تخصصی کارجو (مثلاً «توسعه‌دهنده فرانت‌اند») — مستقل از EducationLevel.</summary>
    public string? JobTitle { get; private set; }

    /// <summary>
    /// خلاصه/بیوگرافی حرفه‌ای («درباره من») — عمداً فیلدی جدا از Interests (علاقه‌مندی‌های شخصی) و
    /// PsychologyAnswers (پاسخ‌های روان‌شناسی) نگه داشته شده تا این سه معنای متفاوت با هم قاطی نشوند.
    /// </summary>
    public string? ProfessionalSummary { get; private set; }

    public string? LinkedInUrl { get; private set; }
    public string? GitHubUrl { get; private set; }
    public string? PersonalWebsiteUrl { get; private set; }

    /// <summary>نوع همکاری موردنظر — دورکار/حضوری/ترکیبی.</summary>
    public PreferredWorkType? PreferredWorkType { get; private set; }

    /// <summary>
    /// حداقل/حداکثر حقوق درخواستی — طبق دستور صریح این فاز به «تومان» ذخیره می‌شوند، برخلاف قرارداد
    /// غالب سیستم که مبالغ مالی را با پسوند InRials ذخیره می‌کند (نظیر Company.WalletBalanceInRials،
    /// PaymentTransaction.AmountInRials). این یک انحراف عمدی و صریح طبق دستور محصولی این فاز است — دقیقاً
    /// مشابه تصمیم BannerSlot.DailyPrice در فاز «مدیریت و رزرو بنرهای تبلیغاتی» — و باید در هرجایی که این
    /// مقدار با مبالغ InRials سیستم ترکیب می‌شود (مثلاً گزارش‌گیری/فیلتر مشترک) به‌صراحت تبدیل واحد شود.
    /// </summary>
    public long? MinRequestedSalaryInToman { get; private set; }

    /// <summary>ر.ک. مستندات <see cref="MinRequestedSalaryInToman"/>.</summary>
    public long? MaxRequestedSalaryInToman { get; private set; }

    /// <summary>
    /// وضعیت جستجوی کار — آیا کارجو فعالانه به‌دنبال فرصت شغلی جدید است. پیش‌فرض true در نظر گرفته شده
    /// (یک پروفایل/رزومه تازه‌ساخته‌شده، به‌طور طبیعی یعنی صاحبش در حال جستجوی کار است)؛ این یک مقدار
    /// واقعی دامنه است نه داده جعلی، اما در صورت نیاز به معنای متفاوت باید توسط محصول تایید شود.
    /// </summary>
    public bool IsActivelyLookingForJob { get; private set; } = true;

    private Candidate() { }

    private Candidate(
        Guid id, Guid userId, string mobileNumber, string fullName, MilitaryServiceStatus militaryServiceStatus,
        string educationLevel, string? workExperienceSummary, string? skills, string? interests, string? psychologyAnswers,
        string? resumeFileUrl, bool isFeePaid, string? email = null, string? city = null, string? jobTitle = null,
        string? professionalSummary = null, string? linkedInUrl = null, string? gitHubUrl = null, string? personalWebsiteUrl = null,
        PreferredWorkType? preferredWorkType = null, long? minRequestedSalaryInToman = null, long? maxRequestedSalaryInToman = null,
        bool isActivelyLookingForJob = true) : base(id)
    {
        UserId = userId;
        MobileNumber = mobileNumber;
        FullName = fullName;
        MilitaryServiceStatus = militaryServiceStatus;
        EducationLevel = educationLevel;
        WorkExperienceSummary = workExperienceSummary;
        Skills = skills;
        Interests = interests;
        PsychologyAnswers = psychologyAnswers;
        ResumeFileUrl = resumeFileUrl;
        IsFeePaid = isFeePaid;
        Email = email;
        City = city;
        JobTitle = jobTitle;
        ProfessionalSummary = professionalSummary;
        LinkedInUrl = linkedInUrl;
        GitHubUrl = gitHubUrl;
        PersonalWebsiteUrl = personalWebsiteUrl;
        PreferredWorkType = preferredWorkType;
        MinRequestedSalaryInToman = minRequestedSalaryInToman;
        MaxRequestedSalaryInToman = maxRequestedSalaryInToman;
        IsActivelyLookingForJob = isActivelyLookingForJob;
    }

    /// <summary>
    /// ایجاد پروفایل/رزومه از طریق رزومه‌ساز. طبق ADR-013 با <c>IsFeePaid=false</c> ساخته می‌شود؛
    /// کارجو باید از طریق <c>SubmitResumeFeePaymentCommand</c> هزینه را بپردازد تا رزومه نهایی/قابل‌ارسال شود.
    /// </summary>
    public static Result<Candidate> Create(
        Guid userId, string mobileNumber, string fullName, MilitaryServiceStatus militaryServiceStatus,
        string educationLevel, string? workExperienceSummary, string? skills, string? interests = null, string? psychologyAnswers = null,
        string? email = null, string? city = null, string? jobTitle = null, string? professionalSummary = null,
        string? linkedInUrl = null, string? gitHubUrl = null, string? personalWebsiteUrl = null, PreferredWorkType? preferredWorkType = null,
        long? minRequestedSalaryInToman = null, long? maxRequestedSalaryInToman = null, bool isActivelyLookingForJob = true)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Failure<Candidate>(Error.Validation("VALIDATION_ERROR", "نام و نام خانوادگی الزامی است."));

        var salaryRangeCheck = ValidateSalaryRange(minRequestedSalaryInToman, maxRequestedSalaryInToman);
        if (salaryRangeCheck.IsFailure)
            return Result.Failure<Candidate>(salaryRangeCheck.Error);

        return Result.Success(new Candidate(
            Guid.NewGuid(), userId, mobileNumber, fullName, militaryServiceStatus, educationLevel, workExperienceSummary, skills,
            interests, psychologyAnswers, null, false, email, city, jobTitle, professionalSummary, linkedInUrl, gitHubUrl,
            personalWebsiteUrl, preferredWorkType, minRequestedSalaryInToman, maxRequestedSalaryInToman, isActivelyLookingForJob));
    }

    private static Result ValidateSalaryRange(long? minRequestedSalaryInToman, long? maxRequestedSalaryInToman)
    {
        if (minRequestedSalaryInToman is < 0 || maxRequestedSalaryInToman is < 0)
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "حقوق درخواستی نمی‌تواند منفی باشد."));

        if (minRequestedSalaryInToman.HasValue && maxRequestedSalaryInToman.HasValue && minRequestedSalaryInToman > maxRequestedSalaryInToman)
            return Result.Failure(Error.Validation("VALIDATION_ERROR", "حداقل حقوق درخواستی نمی‌تواند بیشتر از حداکثر آن باشد."));

        return Result.Success();
    }

    /// <summary>
    /// ایجاد سریع پروفایل حداقلی برای مسیر «ارسال مستقیم رزومه» (بدون تکمیل رزومه‌ساز):
    /// طبق درخواست محصولی، کارجو می‌تواند به‌جای ساخت رزومه کامل، فقط نام و فایل رزومه را ارسال کند.
    /// طبق ADR-013 این مسیر مشمول هزینه رزومه‌ساز نمی‌شود، بنابراین از ابتدا <c>IsFeePaid=true</c> است.
    /// </summary>
    public static Result<Candidate> CreateFromDirectUpload(Guid userId, string mobileNumber, string fullName, string resumeFileUrl)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Failure<Candidate>(Error.Validation("VALIDATION_ERROR", "نام و نام خانوادگی الزامی است."));

        return Result.Success(new Candidate(
            Guid.NewGuid(), userId, mobileNumber, fullName, MilitaryServiceStatus.NotApplicable, string.Empty, null, null, null, null, resumeFileUrl, true));
    }

    public Result UpdateProfile(
        string fullName, MilitaryServiceStatus militaryServiceStatus, string educationLevel, string? workExperienceSummary,
        string? skills, string? interests = null, string? psychologyAnswers = null, string? email = null, string? city = null,
        string? jobTitle = null, string? professionalSummary = null, string? linkedInUrl = null, string? gitHubUrl = null,
        string? personalWebsiteUrl = null, PreferredWorkType? preferredWorkType = null, long? minRequestedSalaryInToman = null,
        long? maxRequestedSalaryInToman = null, bool isActivelyLookingForJob = true)
    {
        var salaryRangeCheck = ValidateSalaryRange(minRequestedSalaryInToman, maxRequestedSalaryInToman);
        if (salaryRangeCheck.IsFailure)
            return salaryRangeCheck;

        FullName = fullName;
        MilitaryServiceStatus = militaryServiceStatus;
        EducationLevel = educationLevel;
        WorkExperienceSummary = workExperienceSummary;
        Skills = skills;
        Interests = interests;
        PsychologyAnswers = psychologyAnswers;
        Email = email;
        City = city;
        JobTitle = jobTitle;
        ProfessionalSummary = professionalSummary;
        LinkedInUrl = linkedInUrl;
        GitHubUrl = gitHubUrl;
        PersonalWebsiteUrl = personalWebsiteUrl;
        PreferredWorkType = preferredWorkType;
        MinRequestedSalaryInToman = minRequestedSalaryInToman;
        MaxRequestedSalaryInToman = maxRequestedSalaryInToman;
        IsActivelyLookingForJob = isActivelyLookingForJob;
        return Result.Success();
    }

    /// <summary>
    /// به‌روزرسانی مستقل ترجیحات شغلی («نوع همکاری»/«محدوده حقوق»/«وضعیت جستجوی کار») — طبق فاز
    /// «پروفایل و رزومه‌ساز کارجو» (PUT /preferences)، مستقل از UpdateProfile نگه داشته شده تا این
    /// بخش از فرم بدون نیاز به ارسال مجدد کل فیلدهای پروفایل (نام/تحصیلات/...) قابل ذخیره باشد.
    /// </summary>
    public Result UpdateJobPreferences(
        PreferredWorkType? preferredWorkType, long? minRequestedSalaryInToman, long? maxRequestedSalaryInToman, bool isActivelyLookingForJob)
    {
        var salaryRangeCheck = ValidateSalaryRange(minRequestedSalaryInToman, maxRequestedSalaryInToman);
        if (salaryRangeCheck.IsFailure)
            return salaryRangeCheck;

        PreferredWorkType = preferredWorkType;
        MinRequestedSalaryInToman = minRequestedSalaryInToman;
        MaxRequestedSalaryInToman = maxRequestedSalaryInToman;
        IsActivelyLookingForJob = isActivelyLookingForJob;
        return Result.Success();
    }

    public void AttachResumeFile(string resumeFileUrl) => ResumeFileUrl = resumeFileUrl;

    /// <summary>تنظیم/جایگزینی تصویر آواتار پس از آپلود موفق فایل.</summary>
    public void SetAvatarUrl(string avatarUrl) => AvatarUrl = avatarUrl;

    /// <summary>طبق ADR-013 — پس از Callback موفق پرداخت هزینه رزومه‌ساز صدا زده می‌شود.</summary>
    public Result MarkFeePaid()
    {
        if (IsFeePaid)
            return Result.Failure(Error.Conflict("FEE_ALREADY_PAID", "هزینه این رزومه قبلاً پرداخت شده است."));

        IsFeePaid = true;
        return Result.Success();
    }
}
