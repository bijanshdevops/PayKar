using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.JobAds.Commands;

public sealed record UpdateJobAdCommand(
    Guid JobAdId,
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

public sealed class UpdateJobAdCommandValidator : AbstractValidator<UpdateJobAdCommand>
{
    public UpdateJobAdCommandValidator()
    {
        RuleFor(x => x.JobAdId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.WorkShift).IsEnumName(typeof(Domain.JobAds.WorkShift));
        RuleFor(x => x.MealPlan).IsEnumName(typeof(Domain.JobAds.MealPlan));
        RuleFor(x => x.SalaryRangeType).IsEnumName(typeof(Domain.JobAds.SalaryRangeType));
        RuleFor(x => x.ContractType).IsEnumName(typeof(Domain.JobAds.ContractType));
        RuleFor(x => x.GenderPreference).IsEnumName(typeof(Domain.JobAds.GenderPreference)).When(x => !string.IsNullOrWhiteSpace(x.GenderPreference));
        RuleFor(x => x.MinEducationLevel).IsEnumName(typeof(Domain.JobAds.EducationLevel)).When(x => !string.IsNullOrWhiteSpace(x.MinEducationLevel));
        RuleFor(x => x.MilitaryServiceStatus).IsEnumName(typeof(Domain.JobAds.MilitaryServiceStatus)).When(x => !string.IsNullOrWhiteSpace(x.MilitaryServiceStatus));
    }
}

public sealed class UpdateJobAdCommandHandler : IRequestHandler<UpdateJobAdCommand, Result<JobAdDto>>
{
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateJobAdCommandHandler(
        IJobAdRepository jobAdRepository, ICompanyRepository companyRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<JobAdDto>> Handle(UpdateJobAdCommand request, CancellationToken cancellationToken)
    {
        var jobAd = await _jobAdRepository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<JobAdDto>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        var ownershipCheck = await JobAdOwnershipGuard.EnsureOwnerAsync(jobAd, _companyRepository, _currentUser, cancellationToken);
        if (ownershipCheck.IsFailure)
            return Result.Failure<JobAdDto>(ownershipCheck.Error);

        var salaryRangeResult = CreateJobAdCommandHandler.BuildSalaryRange(new CreateJobAdCommand(
            request.Title, request.Description, request.WorkShift, request.HasCommuteService, request.CommuteServiceRoutes,
            request.MealPlan, request.InsuranceTypes, request.SalaryRangeType, request.FixedAmount, request.MinAmount, request.MaxAmount,
            request.ContractType));

        if (salaryRangeResult.IsFailure)
            return Result.Failure<JobAdDto>(salaryRangeResult.Error);

        var insuranceTypes = CreateJobAdCommandHandler.ParseInsuranceTypes(request.InsuranceTypes);

        var updateResult = jobAd.Update(
            request.Title,
            request.Description,
            Enum.Parse<Domain.JobAds.WorkShift>(request.WorkShift),
            request.HasCommuteService,
            request.CommuteServiceRoutes,
            Enum.Parse<Domain.JobAds.MealPlan>(request.MealPlan),
            insuranceTypes,
            salaryRangeResult.Value,
            Enum.Parse<Domain.JobAds.ContractType>(request.ContractType),
            CreateJobAdCommandHandler.ParseOptionalEnum(request.GenderPreference, Domain.JobAds.GenderPreference.Any),
            request.MinAge,
            request.MaxAge,
            CreateJobAdCommandHandler.ParseOptionalEnum(request.MinEducationLevel, Domain.JobAds.EducationLevel.Unspecified),
            request.MinExperienceYears,
            CreateJobAdCommandHandler.ParseNullableEnum<Domain.JobAds.MilitaryServiceStatus>(request.MilitaryServiceStatus),
            request.HeadcountNeeded,
            request.ApplicationDeadlineUtc,
            request.RequiredSkills,
            request.AdditionalBenefits);

        if (updateResult.IsFailure)
            return Result.Failure<JobAdDto>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var company = await _companyRepository.GetByIdAsync(jobAd.CompanyId, cancellationToken);
        return Result.Success(JobAdMapper.ToDto(jobAd, company));
    }
}
