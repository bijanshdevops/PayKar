using IndustrialPlatform.Shared.Entities;
using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Domain.Candidates;

/// <summary>
/// درخواست همکاری کارجو برای یک آگهی — Aggregate Root طبق سند 02-Domain-Glossary.md بخش ۴.۲.
/// TrackingToken مطابق بخش ۵.۲ سند ۰۲ (Application Tracking Token): کد رهگیری بدون افشای هویت.
/// </summary>
public sealed class JobApplication : AggregateRoot<Guid>
{
    private static readonly Dictionary<JobApplicationStatus, JobApplicationStatus[]> AllowedTransitions = new()
    {
        [JobApplicationStatus.Submitted] = new[] { JobApplicationStatus.Reviewed, JobApplicationStatus.Rejected },
        [JobApplicationStatus.Reviewed] = new[] { JobApplicationStatus.InterviewScheduled, JobApplicationStatus.Rejected },
        [JobApplicationStatus.InterviewScheduled] = new[] { JobApplicationStatus.Accepted, JobApplicationStatus.Rejected },
        [JobApplicationStatus.Accepted] = Array.Empty<JobApplicationStatus>(),
        [JobApplicationStatus.Rejected] = Array.Empty<JobApplicationStatus>()
    };

    public Guid JobAdId { get; private set; }
    public Guid CandidateId { get; private set; }
    public JobApplicationStatus Status { get; private set; }
    public string TrackingToken { get; private set; } = string.Empty;

    /// <summary>
    /// درصد تطابق (۰ تا ۱۰۰) بین مهارت‌های کارجو و نیازمندی‌های آگهی، در لحظه ثبت درخواست محاسبه
    /// و ذخیره می‌شود (<see cref="IndustrialPlatform.Application.Candidates.ResumeMatchScoreCalculator"/>).
    /// Nullable است چون برای مسیر «ارسال مستقیم» (بدون پروفایل کامل) یا آگهی بدون فیلد RequiredSkills
    /// امکان محاسبه معناداری وجود ندارد.
    /// </summary>
    public int? MatchScorePercent { get; private set; }

    /// <summary>یادداشت داخلی کارفرما دربارهٔ این متقاضی — هرگز به کارجو نمایش داده نمی‌شود (فقط در EmployerApplicantDto).</summary>
    public string? CompanyNotes { get; private set; }

    /// <summary>زمان (UTC) مصاحبهٔ زمان‌بندی‌شده — اختیاری، معمولاً هم‌زمان با انتقال به وضعیت InterviewScheduled ثبت می‌شود.</summary>
    public DateTime? InterviewDateTimeUtc { get; private set; }

    private JobApplication() { }

    private JobApplication(Guid id, Guid jobAdId, Guid candidateId, string trackingToken, int? matchScorePercent) : base(id)
    {
        JobAdId = jobAdId;
        CandidateId = candidateId;
        TrackingToken = trackingToken;
        Status = JobApplicationStatus.Submitted;
        MatchScorePercent = matchScorePercent;
    }

    public static JobApplication Create(Guid jobAdId, Guid candidateId, string trackingToken, int? matchScorePercent = null) =>
        new(Guid.NewGuid(), jobAdId, candidateId, trackingToken, matchScorePercent);

    public Result TransitionTo(JobApplicationStatus newStatus)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            return Result.Failure(Error.Conflict(
                "INVALID_APPLICATION_STATUS_TRANSITION",
                $"انتقال وضعیت از {Status} به {newStatus} مجاز نیست."));
        }

        Status = newStatus;
        return Result.Success();
    }

    /// <summary>
    /// ثبت/به‌روزرسانی یادداشت داخلی و زمان مصاحبه توسط کارفرما — مستقل از تغییر وضعیت
    /// (کارفرما می‌تواند صرفاً یادداشت را بدون تغییر وضعیت به‌روز کند).
    /// </summary>
    public void UpdateEmployerNotes(string? companyNotes, DateTime? interviewDateTimeUtc)
    {
        CompanyNotes = companyNotes;
        InterviewDateTimeUtc = interviewDateTimeUtc;
    }
}
