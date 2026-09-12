namespace IndustrialPlatform.Application.Candidates;

/// <summary>یک ردیف سابقه تحصیلی — طبق فاز «مدیریت رزومه‌ها و متقاضیان» (CandidateEducation).</summary>
public sealed record CandidateEducationDto(
    Guid Id,
    string DegreeLevel,
    string FieldOfStudy,
    string InstitutionName,
    int? GraduationYear);

/// <summary>یک ردیف سابقه شغلی ساختاریافته — طبق فاز «مدیریت رزومه‌ها و متقاضیان» (CandidateWorkExperience).</summary>
public sealed record CandidateWorkExperienceDto(
    Guid Id,
    string JobTitle,
    string CompanyName,
    int StartYear,
    int? EndYear,
    string? Description);

/// <summary>یک ردیف مدرک/گواهینامه — طبق فاز «پروفایل و رزومه‌ساز کارجو» (CandidateCertification).</summary>
public sealed record CandidateCertificationDto(Guid Id, string Title, string IssuingOrganization, int YearObtained);

/// <summary>یک ردیف مهارت ساختاریافته — طبق فاز «پروفایل و رزومه‌ساز کارجو» (CandidateSkill).</summary>
public sealed record CandidateSkillDto(Guid Id, string Name, string Category, int Level);

/// <summary>یک ردیف زبان خارجی — طبق فاز «پروفایل و رزومه‌ساز کارجو» (CandidateLanguage).</summary>
public sealed record CandidateLanguageDto(Guid Id, string Name, string ProficiencyLevel);

public sealed record CandidateDto(
    Guid Id,
    string FullName,
    string MilitaryServiceStatus,
    string EducationLevel,
    string? WorkExperienceSummary,
    string? Skills,
    string? Interests,
    string? PsychologyAnswers,
    string? ResumeFileUrl,
    bool IsFeePaid,
    long ResumeCreationFeeAmountInRials,
    string? AvatarUrl,
    // طبق فاز «مدیریت رزومه‌ها و متقاضیان» — فیلدهای اختیاری تماس + سوابق ساختاریافته.
    string? Email,
    string? City,
    IReadOnlyList<CandidateEducationDto> Educations,
    IReadOnlyList<CandidateWorkExperienceDto> WorkExperiences,
    // ---------- طبق فاز «پروفایل و رزومه‌ساز کارجو» ----------
    string? JobTitle,
    string? ProfessionalSummary,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? PersonalWebsiteUrl,
    string? PreferredWorkType,
    long? MinRequestedSalaryInToman,
    long? MaxRequestedSalaryInToman,
    bool IsActivelyLookingForJob,
    IReadOnlyList<CandidateCertificationDto> Certifications,
    IReadOnlyList<CandidateSkillDto> StructuredSkills,
    IReadOnlyList<CandidateLanguageDto> Languages);

/// <summary>
/// خروجی کامل GetMyCandidateProfileQuery — CandidateDto به‌همراه درصد تکمیل رزومه و پیشنهادهای
/// قطعی/غیرهوش‌مصنوعی برای تکمیل آن (ر.ک. CalculateResumeScoreService).
/// </summary>
public sealed record CandidateProfileDto(CandidateDto Profile, int ResumeCompletionPercent, IReadOnlyList<string> Suggestions);

public sealed record JobApplicationDto(
    Guid Id,
    Guid JobAdId,
    Guid CandidateId,
    string Status,
    string TrackingToken,
    DateTime CreatedAtUtc,
    int? MatchScorePercent);

/// <summary>طبق تسک #75 — ردیف نمایش در لیست «درخواست‌های من» پروفایل کارجو، شامل عنوان آگهی.</summary>
public sealed record MyApplicationDto(
    Guid Id,
    Guid JobAdId,
    string JobAdTitle,
    string Status,
    DateTime CreatedAtUtc);

/// <summary>
/// درخواست همکاری از منظر کارفرمای صاحب آگهی — طبق تصمیم صریح محصولی این فاز: از آنجا که کارجو با
/// ارسال درخواست به‌صورت ارادی برای همین آگهی اپلای کرده، هویت او (نام/آواتار/تحصیلات/سوابق/مهارت‌ها/
/// فایل رزومه) مستقیماً در اختیار همان کارفرما قرار می‌گیرد. این DTO فقط از طریق
/// GetApplicationsForJobAdQuery/GetRecentApplicantsForCompanyQuery بازگردانده می‌شود که هر دو با
/// JobAdOwnershipGuard محدود به صاحب همان آگهی‌اند — هیچ اندپوینتی امکان مرور آزاد بانک کارجویان را
/// به کارفرما نمی‌دهد (مرز حریم خصوصی طبق سند 02-Domain-Glossary.md بخش ۵.۲، محدودشده به همین سطح).
/// </summary>
public sealed record EmployerApplicantDto(
    Guid ApplicationId,
    Guid JobAdId,
    string JobAdTitle,
    Guid CandidateId,
    string CandidateFullName,
    string? CandidateAvatarUrl,
    // طبق تصمیم صریح محصولی فاز «مدیریت رزومه‌ها و متقاضیان»: چون کارجو با ارسال درخواست، ارادی برای
    // همین آگهی Apply کرده (دقیقاً همان استدلالی که سایر فیلدهای هویتی این DTO را توجیه می‌کند)،
    // شماره موبایل واقعی او (فیلد موجود در Candidate، نه فیلد جعلی) نیز در اختیار همان کارفرما قرار
    // می‌گیرد تا امکان تماس مستقیم فراهم شود.
    string CandidateMobileNumber,
    // ایمیل و شهر — طبق تصمیم صریح محصولی این فاز اکنون فیلدهای واقعی و اختیاری روی Candidate هستند؛
    // ممکن است null باشند (کارجویانی که هنوز آن‌ها را پر نکرده‌اند).
    string? CandidateEmail,
    string? CandidateCity,
    string CandidateEducationLevel,
    string? CandidateWorkExperienceSummary,
    // سوابق ساختاریافته جدید — مکمل دو فیلد آزاد بالا، نه جایگزین آن‌ها (ممکن است خالی باشند اگر
    // کارجو هنوز این بخش‌ها را تکمیل نکرده).
    IReadOnlyList<CandidateEducationDto> CandidateEducations,
    IReadOnlyList<CandidateWorkExperienceDto> CandidateWorkExperiences,
    string? CandidateSkills,
    string? CandidateResumeFileUrl,
    int? MatchScorePercent,
    string Status,
    string TrackingToken,
    DateTime CreatedAtUtc,
    string? CompanyNotes,
    DateTime? InterviewDateTimeUtc);

/// <summary>
/// خروجی GetJobApplicationResumeQuery — فقط مسیر نسبی فایل (برای resolve به مسیر فیزیکی توسط
/// IFileStorageService در لایه Api) و نام پیشنهادی فایل هنگام دانلود، بدون افشای مسیر فیزیکی دیسک.
/// </summary>
public sealed record ResumeDownloadDto(string RelativeFileUrl, string SuggestedFileName);
