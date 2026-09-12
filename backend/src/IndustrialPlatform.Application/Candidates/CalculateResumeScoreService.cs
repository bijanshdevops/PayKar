using IndustrialPlatform.Domain.Candidates;

namespace IndustrialPlatform.Application.Candidates;

/// <summary>
/// محاسبهٔ درصد تکمیل پروفایل/رزومه کارجو و تولید فهرست پیشنهادهای تکمیل — طبق فاز «پروفایل و
/// رزومه‌ساز کارجو». دقیقاً هم‌راستا با فلسفهٔ <see cref="ResumeMatchScoreCalculator"/>: این سرویس
/// عمداً ساده، قطعی (Deterministic) و بدون وابستگی خارجی/هوش مصنوعی واقعی است — برچسب «پیشنهادات
/// هوشمند» فقط در رابط کاربری به‌کار می‌رود؛ منطق این کلاس صرفاً یک وزن‌دهی قطعی روی داده واقعی
/// پروفایل است، نه فراخوانی به یک سرویس LLM (که در این پروژه اصلاً زیرساختی برایش وجود ندارد).
/// خالص و بدون وابستگی (Pure) است تا به‌سادگی Unit-Test پذیر باشد.
/// وزن‌دهی طبق دستور صریح محصولی این فاز: عکس پروفایل ۱۰٪، سابقه کار ۳۰٪، مهارت‌ها ۲۰٪، تحصیلات ۱۵٪،
/// درباره من ۱۰٪، زبان و ترجیحات ۱۵٪ (جمعاً ۱۰۰٪).
/// </summary>
public static class CalculateResumeScoreService
{
    private const int AvatarWeight = 10;
    private const int WorkExperienceWeight = 30;
    private const int SkillsWeight = 20;
    private const int EducationWeight = 15;
    private const int SummaryWeight = 10;
    private const int LanguageAndPreferencesWeight = 15;

    /// <summary>حداکثر تعداد سابقه شغلی که برای رسیدن به وزن کامل این بخش لازم است (بیشتر از این تاثیری در امتیاز ندارد).</summary>
    private const int FullScoreWorkExperienceCount = 2;

    /// <summary>حداکثر تعداد مهارت که برای رسیدن به وزن کامل این بخش لازم است.</summary>
    private const int FullScoreSkillCount = 5;

    public static (int CompletionPercent, IReadOnlyList<string> Suggestions) Calculate(
        Candidate candidate,
        IReadOnlyList<CandidateEducation> educations,
        IReadOnlyList<CandidateWorkExperience> workExperiences,
        IReadOnlyList<CandidateSkill> skills,
        IReadOnlyList<CandidateLanguage> languages)
    {
        var suggestions = new List<string>();
        var total = 0;

        // ۱. عکس پروفایل — ۱۰٪ (باینری: دارد/ندارد).
        if (!string.IsNullOrWhiteSpace(candidate.AvatarUrl))
            total += AvatarWeight;
        else
            suggestions.Add("عکس پروفایل خود را اضافه کنید تا اعتبار رزومه شما افزایش یابد.");

        // ۲. سابقه کار — ۳۰٪ (پلکانی: هر ردیف نسبت به سقف FullScoreWorkExperienceCount).
        var workExperienceScore = (int)Math.Round(
            Math.Min(workExperiences.Count, FullScoreWorkExperienceCount) * WorkExperienceWeight / (double)FullScoreWorkExperienceCount,
            MidpointRounding.AwayFromZero);
        total += workExperienceScore;
        if (workExperiences.Count < FullScoreWorkExperienceCount)
            suggestions.Add("سوابق شغلی بیشتری اضافه کنید.");

        // ۳. مهارت‌ها — ۲۰٪ (پلکانی: هر مهارت نسبت به سقف FullScoreSkillCount).
        var skillsScore = (int)Math.Round(
            Math.Min(skills.Count, FullScoreSkillCount) * SkillsWeight / (double)FullScoreSkillCount,
            MidpointRounding.AwayFromZero);
        total += skillsScore;
        if (skills.Count < FullScoreSkillCount)
            suggestions.Add("بخش مهارت‌ها را تکمیل کنید.");

        // ۴. تحصیلات — ۱۵٪ (باینری: حداقل یک ردیف).
        if (educations.Count > 0)
            total += EducationWeight;
        else
            suggestions.Add("سابقه تحصیلی خود را اضافه کنید.");

        // ۵. درباره من — ۱۰٪ (باینری: متن خالی/غیرخالی).
        if (!string.IsNullOrWhiteSpace(candidate.ProfessionalSummary))
            total += SummaryWeight;
        else
            suggestions.Add("بخش «درباره من» را تکمیل کنید.");

        // ۶. زبان و ترجیحات — ۱۵٪ (تقسیم مساوی بین دو زیر-بخش: حداقل یک زبان، و حداقل یک ترجیح شغلی ثبت‌شده).
        const int halfWeight = LanguageAndPreferencesWeight / 2; // 7
        var otherHalfWeight = LanguageAndPreferencesWeight - halfWeight; // 8

        if (languages.Count > 0)
            total += otherHalfWeight;
        else
            suggestions.Add("حداقل یک زبان خارجی به پروفایل خود اضافه کنید.");

        var hasJobPreferences = candidate.PreferredWorkType.HasValue
            || candidate.MinRequestedSalaryInToman.HasValue
            || candidate.MaxRequestedSalaryInToman.HasValue;
        if (hasJobPreferences)
            total += halfWeight;
        else
            suggestions.Add("ترجیحات شغلی (نوع همکاری و محدوده حقوق درخواستی) را مشخص کنید.");

        return (Math.Clamp(total, 0, 100), suggestions);
    }
}
