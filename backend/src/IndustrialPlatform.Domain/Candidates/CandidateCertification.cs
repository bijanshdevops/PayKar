using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Candidates;

/// <summary>
/// یک ردیف مدرک/گواهینامه کارجو (عنوان/موسسه صادرکننده/سال اخذ) — طبق فاز «پروفایل و رزومه‌ساز
/// کارجو». Aggregate جداگانه از Candidate، هم‌راستا با الگوی CandidateEducation/CandidateWorkExperience
/// (فقط افزوده/حذف منطقی می‌شود، بدون ویرایش درجا).
/// برخلاف CandidateEducation/CandidateWorkExperience (که فاقد FK واقعی در دیتابیس هستند)، اینجا طبق
/// درخواست صریح این فاز یک رابطه Foreign Key واقعی با Cascade Delete به Candidate تعریف شده — هم‌راستا
/// با الگوی BannerDailyStat.BannerAd (جدیدترین و دقیق‌ترین الگوی این نوع رابطه در پروژه).
/// </summary>
public sealed class CandidateCertification : BaseEntity<Guid>
{
    public Guid CandidateId { get; private set; }
    public Candidate? Candidate { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string IssuingOrganization { get; private set; } = string.Empty;

    /// <summary>سال اخذ مدرک به تقویم شمسی (مثلاً ۱۴۰۱).</summary>
    public int YearObtained { get; private set; }

    private CandidateCertification() { }

    private CandidateCertification(Guid id, Guid candidateId, string title, string issuingOrganization, int yearObtained) : base(id)
    {
        CandidateId = candidateId;
        Title = title;
        IssuingOrganization = issuingOrganization;
        YearObtained = yearObtained;
    }

    public static CandidateCertification Create(Guid candidateId, string title, string issuingOrganization, int yearObtained) =>
        new(Guid.NewGuid(), candidateId, title, issuingOrganization, yearObtained);
}
