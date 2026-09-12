namespace IndustrialPlatform.Application.JobAds;

public sealed record SalaryRangeDto(string Type, decimal? FixedAmount, decimal? MinAmount, decimal? MaxAmount);

public sealed record JobAdDto(
    Guid Id,
    Guid CompanyId,
    // طبق ADR-011 — برای نمایش نام شرکت و دکمه «تماس با کارفرما» در صفحه جزئیات آگهی.
    string CompanyName,
    string? CompanyContactPhoneNumber,
    // طبق تسک #94 — برای نمایش لوگو و نشان «تایید شده» شرکت در کارت آگهی صفحه اصلی (داده واقعی، بدون فیلد جعلی).
    string? CompanyLogoUrl,
    bool IsCompanyVerified,
    Guid IndustrialZoneId,
    string Title,
    string Description,
    string WorkShift,
    bool HasCommuteService,
    string? CommuteServiceRoutes,
    string MealPlan,
    IReadOnlyList<string> InsuranceTypes,
    SalaryRangeDto SalaryRange,
    string ContractType,
    string GenderPreference,
    int? MinAge,
    int? MaxAge,
    string MinEducationLevel,
    int? MinExperienceYears,
    string? MilitaryServiceStatus,
    int? HeadcountNeeded,
    DateTime? ApplicationDeadlineUtc,
    string? RequiredSkills,
    string? AdditionalBenefits,
    string Status,
    bool IsFeePaid,
    string? RejectionReason,
    long ListingFeeAmountInRials,
    DateTime? SubmittedForReviewAtUtc,
    DateTime? PublishedAtUtc,
    DateTime? ExpiresAtUtc,
    // طبق تصمیم صریح محصولی فاز جدید — بازدید واقعی (RecordJobAdViewCommand) و فلگ ویژهٔ ادمین (بدون پلن پرداختی).
    int ViewsCount,
    bool IsFeatured);
