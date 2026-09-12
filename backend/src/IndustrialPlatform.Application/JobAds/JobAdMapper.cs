using IndustrialPlatform.Domain.Companies;
using IndustrialPlatform.Domain.JobAds;

namespace IndustrialPlatform.Application.JobAds;

internal static class JobAdMapper
{
    /// <summary>
    /// طبق ADR-011: پارامتر <paramref name="company"/> اختیاری است — در مسیرهایی که شرکت هنوز بارگذاری نشده
    /// (یا حذف شده) مقدار null پذیرفته می‌شود و فیلدهای مربوط به شرکت با مقادیر خالی/جایگزین پر می‌شوند.
    /// </summary>
    public static JobAdDto ToDto(JobAd jobAd, Company? company = null) => new(
        jobAd.Id,
        jobAd.CompanyId,
        company?.Name ?? "شرکت حذف‌شده",
        company?.ContactPhoneNumber,
        company?.LogoUrl,
        company?.VerificationStatus == VerificationStatus.Verified,
        jobAd.IndustrialZoneId,
        jobAd.Title,
        jobAd.Description,
        jobAd.WorkShift.ToString(),
        jobAd.HasCommuteService,
        jobAd.CommuteServiceRoutes,
        jobAd.MealPlan.ToString(),
        SplitInsuranceFlags(jobAd.InsuranceTypes),
        new SalaryRangeDto(jobAd.SalaryRange.Type.ToString(), jobAd.SalaryRange.FixedAmount, jobAd.SalaryRange.MinAmount, jobAd.SalaryRange.MaxAmount),
        jobAd.ContractType.ToString(),
        jobAd.GenderPreference.ToString(),
        jobAd.MinAge,
        jobAd.MaxAge,
        jobAd.MinEducationLevel.ToString(),
        jobAd.MinExperienceYears,
        jobAd.MilitaryServiceStatus?.ToString(),
        jobAd.HeadcountNeeded,
        jobAd.ApplicationDeadlineUtc,
        jobAd.RequiredSkills,
        jobAd.AdditionalBenefits,
        jobAd.Status.ToString(),
        jobAd.IsFeePaid,
        jobAd.RejectionReason,
        JobAd.ListingFeeAmountInRials,
        jobAd.SubmittedForReviewAtUtc,
        jobAd.PublishedAtUtc,
        jobAd.ExpiresAtUtc,
        jobAd.ViewsCount,
        jobAd.IsFeatured);

    private static IReadOnlyList<string> SplitInsuranceFlags(InsuranceType value) =>
        Enum.GetValues<InsuranceType>()
            .Where(flag => flag != InsuranceType.None && value.HasFlag(flag))
            .Select(flag => flag.ToString())
            .ToList();
}
