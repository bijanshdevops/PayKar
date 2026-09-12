using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Candidates;

/// <summary>
/// یک ردیف زبان خارجی و سطح تسلط کارجو — طبق فاز «پروفایل و رزومه‌ساز کارجو».
/// </summary>
public sealed class CandidateLanguage : BaseEntity<Guid>
{
    public Guid CandidateId { get; private set; }
    public Candidate? Candidate { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public LanguageProficiencyLevel ProficiencyLevel { get; private set; }

    private CandidateLanguage() { }

    private CandidateLanguage(Guid id, Guid candidateId, string name, LanguageProficiencyLevel proficiencyLevel) : base(id)
    {
        CandidateId = candidateId;
        Name = name;
        ProficiencyLevel = proficiencyLevel;
    }

    public static CandidateLanguage Create(Guid candidateId, string name, LanguageProficiencyLevel proficiencyLevel) =>
        new(Guid.NewGuid(), candidateId, name, proficiencyLevel);

    /// <summary>
    /// به‌روزرسانی درجای سطح تسلط — طبق فاز «پروفایل و رزومه‌ساز کارجو» (UpdateCandidateLanguagesCommand):
    /// وقتی نام زبان در Sync جدید هنوز موجود است اما سطح تسلط تغییر کرده، به‌جای حذف+ایجاد مجدد (که با
    /// ایندکس یکتای candidate_id+name در همان تراکنش تداخل می‌کرد) این ردیف موجود به‌روزرسانی می‌شود.
    /// </summary>
    public void UpdateProficiencyLevel(LanguageProficiencyLevel proficiencyLevel) => ProficiencyLevel = proficiencyLevel;
}
