using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Candidates;

/// <summary>
/// یک ردیف سابقه تحصیلی کارجو (مقطع/رشته/دانشگاه/سال فارغ‌التحصیلی) — طبق فاز «مدیریت رزومه‌ها و
/// متقاضیان». Aggregate جداگانه از Candidate نگه داشته شده، هم‌راستا با الگوی CompanyDocument
/// (چرخه حیات مستقل: فقط افزوده/حذف منطقی می‌شود، بدون ویرایش درجا).
/// DegreeLevel عمداً متن آزاد است (نه Enum ثابت JobAds.EducationLevel) تا با فیلد موجود
/// Candidate.EducationLevel (که خود متن آزاد است) هم‌راستا بماند.
/// </summary>
public sealed class CandidateEducation : BaseEntity<Guid>
{
    public Guid CandidateId { get; private set; }
    public string DegreeLevel { get; private set; } = string.Empty;
    public string FieldOfStudy { get; private set; } = string.Empty;
    public string InstitutionName { get; private set; } = string.Empty;

    /// <summary>سال فارغ‌التحصیلی به تقویم شمسی (مثلاً ۱۴۰۱) — null یعنی هنوز در حال تحصیل.</summary>
    public int? GraduationYear { get; private set; }

    private CandidateEducation() { }

    private CandidateEducation(Guid id, Guid candidateId, string degreeLevel, string fieldOfStudy, string institutionName, int? graduationYear) : base(id)
    {
        CandidateId = candidateId;
        DegreeLevel = degreeLevel;
        FieldOfStudy = fieldOfStudy;
        InstitutionName = institutionName;
        GraduationYear = graduationYear;
    }

    public static CandidateEducation Create(Guid candidateId, string degreeLevel, string fieldOfStudy, string institutionName, int? graduationYear = null) =>
        new(Guid.NewGuid(), candidateId, degreeLevel, fieldOfStudy, institutionName, graduationYear);
}
