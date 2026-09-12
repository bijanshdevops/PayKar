using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Domain.Companies;
using IndustrialPlatform.Domain.JobAds;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.JobAds.Commands;

/// <summary>
/// ثبت آگهی جدید. طبق تصمیم محصولی «فرم کامل با حداقل فیلد الزامی»:
/// الزامی → Title, Description, WorkShift, MealPlan, SalaryRangeType, ContractType.
/// اختیاری → مابقی فیلدهای تکمیلی استخدام (جنسیت، سن، تحصیلات، سابقه کار، سربازی، تعداد نیرو، مهلت ارسال، مهارت‌ها، مزایا).
/// آگهی در وضعیت Draft ایجاد می‌شود و تا پرداخت هزینه ثابت (JobAd.ListingFeeAmountInRials) و تایید ادمین منتشر نخواهد شد.
/// </summary>
public sealed record CreateJobAdCommand(
    string Title,
    string Description,
    string WorkShift,
    bool HasCommuteService,
    string? CommuteServiceRoutes,
    string MealPlan,
    IReadOnlyList<string> InsuranceTypes,
    string SalaryRangeType,
    decimal? FixedAmount,
    decimal? MinAmount,
    decimal? MaxAmount,
    string ContractType,
    string? GenderPreference = null,
    int? MinAge = null,
    int? MaxAge = null,
    string? MinEducationLevel = null,
    int? MinExperienceYears = null,
    string? MilitaryServiceStatus = null,
    int? HeadcountNeeded = null,
    DateTime? ApplicationDeadlineUtc = null,
    string? RequiredSkills = null,
    string? AdditionalBenefits = null) : IRequest<Result<JobAdDto>>;

public sealed class CreateJobAdCommandValidator : AbstractValidator<CreateJobAdCommand>
{
    public CreateJobAdCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان آگهی الزامی است.").MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().WithMessage("شرح آگهی الزامی است.");
        RuleFor(x => x.WorkShift).IsEnumName(typeof(Domain.JobAds.WorkShift)).WithMessage("شیفت کاری نامعتبر است.");
        RuleFor(x => x.MealPlan).IsEnumName(typeof(Domain.JobAds.MealPlan)).WithMessage("نوع وعده غذایی نامعتبر است.");
        RuleFor(x => x.SalaryRangeType).IsEnumName(typeof(SalaryRangeType)).WithMessage("نوع بازه حقوق نامعتبر است.");
        RuleFor(x => x.ContractType).IsEnumName(typeof(Domain.JobAds.ContractType)).WithMessage("نوع قرارداد الزامی و باید معتبر باشد.");

        RuleFor(x => x.GenderPreference).IsEnumName(typeof(Domain.JobAds.GenderPreference))
            .When(x => !string.IsNullOrWhiteSpace(x.GenderPreference)).WithMessage("ترجیح جنسیت نامعتبر است.");
        RuleFor(x => x.MinEducationLevel).IsEnumName(typeof(Domain.JobAds.EducationLevel))
            .When(x => !string.IsNullOrWhiteSpace(x.MinEducationLevel)).WithMessage("حداقل مدرک تحصیلی نامعتبر است.");
        RuleFor(x => x.MilitaryServiceStatus).IsEnumName(typeof(Domain.JobAds.MilitaryServiceStatus))
            .When(x => !string.IsNullOrWhiteSpace(x.MilitaryServiceStatus)).WithMessage("وضعیت خدمت سربازی نامعتبر است.");
        RuleFor(x => x.MinAge).GreaterThan(14).When(x => x.MinAge.HasValue).WithMessage("حداقل سن نامعتبر است.");
        RuleFor(x => x.MaxAge).LessThanOrEqualTo(75).When(x => x.MaxAge.HasValue).WithMessage("حداکثر سن نامعتبر است.");
        RuleFor(x => x.HeadcountNeeded).GreaterThan(0).When(x => x.HeadcountNeeded.HasValue).WithMessage("تعداد نیروی موردنیاز باید بزرگ‌تر از صفر باشد.");
    }
}

public sealed class CreateJobAdCommandHandler : IRequestHandler<CreateJobAdCommand, Result<JobAdDto>>
{
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateJobAdCommandHandler(
        IJobAdRepository jobAdRepository,
        ICompanyRepository companyRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<JobAdDto>> Handle(CreateJobAdCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<JobAdDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var company = await _companyRepository.GetByOwnerUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (company is null)
            return Result.Failure<JobAdDto>(Error.NotFound("COMPANY_NOT_FOUND", "ابتدا باید پروفایل شرکت خود را ثبت کنید."));

        // احراز هویت شرکت — طبق درخواست کاربر: پیش از ثبت آگهی، شرکت باید مدارک هویتی/ثبتی
        // ارسال کرده و در استعلام ادمین تایید شده باشد.
        if (company.VerificationStatus != VerificationStatus.Verified)
            return Result.Failure<JobAdDto>(Error.Forbidden("COMPANY_NOT_VERIFIED", "فقط شرکت‌های احراز هویت‌شده می‌توانند آگهی ثبت کنند. ابتدا مدارک شرکت خود را برای بررسی ارسال کنید."));

        var salaryRangeResult = BuildSalaryRange(request);
        if (salaryRangeResult.IsFailure)
            return Result.Failure<JobAdDto>(salaryRangeResult.Error);

        var insuranceTypes = ParseInsuranceTypes(request.InsuranceTypes);

        var jobAdResult = JobAd.Create(
            company.Id,
            company.IndustrialZoneId,
            request.Title,
            request.Description,
            Enum.Parse<Domain.JobAds.WorkShift>(request.WorkShift),
            request.HasCommuteService,
            request.CommuteServiceRoutes,
            Enum.Parse<Domain.JobAds.MealPlan>(request.MealPlan),
            insuranceTypes,
            salaryRangeResult.Value,
            Enum.Parse<Domain.JobAds.ContractType>(request.ContractType),
            ParseOptionalEnum(request.GenderPreference, Domain.JobAds.GenderPreference.Any),
            request.MinAge,
            request.MaxAge,
            ParseOptionalEnum(request.MinEducationLevel, Domain.JobAds.EducationLevel.Unspecified),
            request.MinExperienceYears,
            ParseNullableEnum<Domain.JobAds.MilitaryServiceStatus>(request.MilitaryServiceStatus),
            request.HeadcountNeeded,
            request.ApplicationDeadlineUtc,
            request.RequiredSkills,
            request.AdditionalBenefits);

        if (jobAdResult.IsFailure)
            return Result.Failure<JobAdDto>(jobAdResult.Error);

        _jobAdRepository.Add(jobAdResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(JobAdMapper.ToDto(jobAdResult.Value, company));
    }

    internal static Result<SalaryRange> BuildSalaryRange(CreateJobAdCommand request)
    {
        var type = Enum.Parse<SalaryRangeType>(request.SalaryRangeType);

        return type switch
        {
            SalaryRangeType.MinistryOfLabor => SalaryRange.MinistryOfLabor(),
            SalaryRangeType.Agreement => SalaryRange.Agreement(),
            SalaryRangeType.FixedAmount => request.FixedAmount.HasValue
                ? SalaryRange.Fixed(request.FixedAmount.Value)
                : Result.Failure<SalaryRange>(Error.Validation("VALIDATION_ERROR", "مبلغ ثابت حقوق الزامی است.")),
            SalaryRangeType.Range => request.MinAmount.HasValue && request.MaxAmount.HasValue
                ? SalaryRange.Range(request.MinAmount.Value, request.MaxAmount.Value)
                : Result.Failure<SalaryRange>(Error.Validation("VALIDATION_ERROR", "کف و سقف بازه حقوق الزامی است.")),
            _ => Result.Failure<SalaryRange>(Error.Validation("VALIDATION_ERROR", "نوع بازه حقوق نامعتبر است."))
        };
    }

    internal static Domain.JobAds.InsuranceType ParseInsuranceTypes(IReadOnlyList<string> values)
    {
        var result = Domain.JobAds.InsuranceType.None;
        foreach (var value in values)
        {
            if (Enum.TryParse<Domain.JobAds.InsuranceType>(value, out var parsed))
                result |= parsed;
        }
        return result;
    }

    internal static TEnum ParseOptionalEnum<TEnum>(string? value, TEnum defaultValue) where TEnum : struct, Enum =>
        !string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value, out var parsed) ? parsed : defaultValue;

    internal static TEnum? ParseNullableEnum<TEnum>(string? value) where TEnum : struct, Enum =>
        !string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value, out var parsed) ? parsed : null;
}
