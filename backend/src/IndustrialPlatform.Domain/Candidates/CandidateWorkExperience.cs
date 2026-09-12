using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Candidates;

/// <summary>
/// یک ردیف سابقه شغلی ساختاریافته کارجو (سمت/شرکت/بازه زمانی) — طبق فاز «مدیریت رزومه‌ها و
/// متقاضیان». مکمل فیلد آزاد Candidate.WorkExperienceSummary است، نه جایگزین آن: کارجویانی که هنوز
/// این بخش را تکمیل نکرده‌اند همچنان همان خلاصه متنی را دارند. Aggregate جداگانه، هم‌راستا با الگوی
/// CompanyDocument/CandidateEducation (فقط افزوده/حذف منطقی می‌شود).
/// </summary>
public sealed class CandidateWorkExperience : BaseEntity<Guid>
{
    public Guid CandidateId { get; private set; }
    public string JobTitle { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;

    /// <summary>سال شروع به تقویم شمسی (مثلاً ۱۳۹۹).</summary>
    public int StartYear { get; private set; }

    /// <summary>سال پایان به تقویم شمسی — null یعنی «اکنون» (شغل فعلی).</summary>
    public int? EndYear { get; private set; }

    public string? Description { get; private set; }

    private CandidateWorkExperience() { }

    private CandidateWorkExperience(Guid id, Guid candidateId, string jobTitle, string companyName, int startYear, int? endYear, string? description) : base(id)
    {
        CandidateId = candidateId;
        JobTitle = jobTitle;
        CompanyName = companyName;
        StartYear = startYear;
        EndYear = endYear;
        Description = description;
    }

    public static CandidateWorkExperience Create(Guid candidateId, string jobTitle, string companyName, int startYear, int? endYear = null, string? description = null) =>
        new(Guid.NewGuid(), candidateId, jobTitle, companyName, startYear, endYear, description);

    /// <summary>
    /// ویرایش درجای یک ردیف سابقه شغلی موجود — طبق فاز «پروفایل و رزومه‌ساز کارجو» (UpsertWorkExperienceCommand).
    /// این متد آگاهانه این Entity را از قاعده قبلی «فقط افزوده/حذف منطقی، بدون ویرایش درجا» (ر.ک. مستندات
    /// بالای کلاس) خارج می‌کند — طبق درخواست صریح محصولی این فاز برای پشتیبانی از ویرایش سوابق شغلی.
    /// </summary>
    public void Update(string jobTitle, string companyName, int startYear, int? endYear, string? description)
    {
        JobTitle = jobTitle;
        CompanyName = companyName;
        StartYear = startYear;
        EndYear = endYear;
        Description = description;
    }
}
