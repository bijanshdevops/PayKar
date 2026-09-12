using IndustrialPlatform.Shared.Entities;

namespace IndustrialPlatform.Domain.Candidates;

/// <summary>
/// یک ردیف مهارت ساختاریافته کارجو (نام/دسته‌بندی/سطح تسلط) — طبق فاز «پروفایل و رزومه‌ساز کارجو».
/// مکمل فیلد آزاد Candidate.Skills است، نه جایگزین آن: کارجویانی که هنوز این بخش را تکمیل نکرده‌اند
/// همچنان همان خلاصه متنی را دارند (هم‌راستا با رابطه Candidate.WorkExperienceSummary در برابر
/// CandidateWorkExperience ساختاریافته).
/// یادداشت طراحی — Category عمداً متن آزاد است، نه Enum ثابت: مقادیر نمونه («Frontend»، «Tools»،
/// «Soft Skills») صرفاً برگرفته از موکاپ یک کارجوی توسعه‌دهنده هستند و برای مشاغل صنعتی/غیرنرم‌افزاری
/// این پلتفرم (که موضوع اصلی IndustrialPlatform است) معنا ندارند؛ Enum ثابت کردن این فیلد باعث
/// قفل‌شدن دامنه به یک نمونه شغلی خاص می‌شد.
/// یادداشت — اعتبارسنجی محدوده Level (۱ تا ۵) در این لایه اعمال نشده، هم‌راستا با الگوی سایر
/// Entityهای همسایه (CandidateEducation/CandidateWorkExperience) که ولیدیشن را به FluentValidation
/// در لایه Application/Commands (که در این فاز ساخته نمی‌شود) واگذار می‌کنند.
/// </summary>
public sealed class CandidateSkill : BaseEntity<Guid>
{
    public Guid CandidateId { get; private set; }
    public Candidate? Candidate { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;

    /// <summary>سطح تسلط از ۱ (مبتدی) تا ۵ (تسلط کامل).</summary>
    public int Level { get; private set; }

    private CandidateSkill() { }

    private CandidateSkill(Guid id, Guid candidateId, string name, string category, int level) : base(id)
    {
        CandidateId = candidateId;
        Name = name;
        Category = category;
        Level = level;
    }

    public static CandidateSkill Create(Guid candidateId, string name, string category, int level) =>
        new(Guid.NewGuid(), candidateId, name, category, level);
}
