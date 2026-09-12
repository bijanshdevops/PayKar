using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Application.Companies;
using IndustrialPlatform.Application.JobAds;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Api;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Queries;

/// <summary>
/// دریافت پروفایل/رزومه کامل کارجوی واردشده — به‌همراه مدارک/مهارت‌ها/زبان‌ها و درصد تکمیل +
/// پیشنهادهای تکمیل (ر.ک. CalculateResumeScoreService). طبق فاز «پروفایل و رزومه‌ساز کارجو» —
/// همان Query از قبل موجود (GetMyCandidateProfileQuery) عمداً غنی‌سازی شده به‌جای ساخت یک
/// GetCandidateProfileQuery مجزا و تکراری با هدف یکسان.
/// </summary>
public sealed record GetMyCandidateProfileQuery : IRequest<Result<CandidateProfileDto>>;

public sealed class GetMyCandidateProfileQueryHandler : IRequestHandler<GetMyCandidateProfileQuery, Result<CandidateProfileDto>>
{
    private readonly ICandidateRepository _repository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly ICandidateCertificationRepository _certificationRepository;
    private readonly ICandidateSkillRepository _skillRepository;
    private readonly ICandidateLanguageRepository _languageRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyCandidateProfileQueryHandler(
        ICandidateRepository repository,
        ICandidateEducationRepository educationRepository,
        ICandidateWorkExperienceRepository workExperienceRepository,
        ICandidateCertificationRepository certificationRepository,
        ICandidateSkillRepository skillRepository,
        ICandidateLanguageRepository languageRepository,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _educationRepository = educationRepository;
        _workExperienceRepository = workExperienceRepository;
        _certificationRepository = certificationRepository;
        _skillRepository = skillRepository;
        _languageRepository = languageRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<CandidateProfileDto>> Handle(GetMyCandidateProfileQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CandidateProfileDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _repository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Failure<CandidateProfileDto>(Error.NotFound("CANDIDATE_NOT_FOUND", "شما هنوز پروفایل/رزومه‌ای ثبت نکرده‌اید."));

        var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var certifications = await _certificationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var skills = await _skillRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var languages = await _languageRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        var profileDto = CandidateMapper.ToDto(candidate, educations, workExperiences, certifications, skills, languages);
        var (completionPercent, suggestions) = CalculateResumeScoreService.Calculate(candidate, educations, workExperiences, skills, languages);

        return Result.Success(new CandidateProfileDto(profileDto, completionPercent, suggestions));
    }
}

/// <summary>طبق تسک #75 — تمام درخواست‌های ارسالی کارجوی واردشده، به‌همراه عنوان آگهی هر درخواست.</summary>
public sealed record GetMyApplicationsQuery : IRequest<Result<IReadOnlyList<MyApplicationDto>>>;

public sealed class GetMyApplicationsQueryHandler : IRequestHandler<GetMyApplicationsQuery, Result<IReadOnlyList<MyApplicationDto>>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyApplicationsQueryHandler(
        ICandidateRepository candidateRepository,
        IJobApplicationRepository applicationRepository,
        IJobAdRepository jobAdRepository,
        ICurrentUserService currentUser)
    {
        _candidateRepository = candidateRepository;
        _applicationRepository = applicationRepository;
        _jobAdRepository = jobAdRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<MyApplicationDto>>> Handle(GetMyApplicationsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<IReadOnlyList<MyApplicationDto>>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Success<IReadOnlyList<MyApplicationDto>>(Array.Empty<MyApplicationDto>());

        var applications = await _applicationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        var result = new List<MyApplicationDto>(applications.Count);
        foreach (var application in applications)
        {
            var jobAd = await _jobAdRepository.GetByIdAsync(application.JobAdId, cancellationToken);
            result.Add(new MyApplicationDto(
                application.Id,
                application.JobAdId,
                jobAd?.Title ?? "آگهی حذف‌شده",
                application.Status.ToString(),
                application.CreatedAtUtc));
        }

        return Result.Success<IReadOnlyList<MyApplicationDto>>(result);
    }
}

/// <summary>پیگیری بدون نیاز به ورود — طبق سند 02-Domain-Glossary.md بخش ۵.۲ (Privacy by design).</summary>
public sealed record GetApplicationByTrackingTokenQuery(string TrackingToken) : IRequest<Result<JobApplicationDto>>;

public sealed class GetApplicationByTrackingTokenQueryHandler : IRequestHandler<GetApplicationByTrackingTokenQuery, Result<JobApplicationDto>>
{
    private readonly IJobApplicationRepository _repository;

    public GetApplicationByTrackingTokenQueryHandler(IJobApplicationRepository repository) => _repository = repository;

    public async Task<Result<JobApplicationDto>> Handle(GetApplicationByTrackingTokenQuery request, CancellationToken cancellationToken)
    {
        var application = await _repository.GetByTrackingTokenAsync(request.TrackingToken.Trim().ToUpperInvariant(), cancellationToken);
        return application is null
            ? Result.Failure<JobApplicationDto>(Error.NotFound("APPLICATION_NOT_FOUND", "درخواستی با این کد رهگیری یافت نشد."))
            : Result.Success(CandidateMapper.ToDto(application));
    }
}

/// <summary>
/// لیست متقاضیان یک آگهی مشخص از منظر کارفرمای صاحب آن — طبق تصمیم صریح محصولی، هویت کامل کارجویانی
/// که ارادی برای همین آگهی Apply کرده‌اند در این پاسخ افشا می‌شود (نام/آواتار/تحصیلات/سوابق/رزومه/
/// درصد تطابق). JobAdOwnershipGuard تضمین می‌کند فقط صاحب همان آگهی به این داده دسترسی دارد.
/// </summary>
public sealed record GetApplicationsForJobAdQuery(Guid JobAdId, int Page, int PageSize) : IRequest<Result<PagedResult<EmployerApplicantDto>>>;

public sealed class GetApplicationsForJobAdQueryHandler : IRequestHandler<GetApplicationsForJobAdQuery, Result<PagedResult<EmployerApplicantDto>>>
{
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly ICurrentUserService _currentUser;

    public GetApplicationsForJobAdQueryHandler(
        IJobApplicationRepository applicationRepository, IJobAdRepository jobAdRepository,
        ICompanyRepository companyRepository, ICandidateRepository candidateRepository,
        ICandidateEducationRepository educationRepository, ICandidateWorkExperienceRepository workExperienceRepository,
        ICurrentUserService currentUser)
    {
        _applicationRepository = applicationRepository;
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _candidateRepository = candidateRepository;
        _educationRepository = educationRepository;
        _workExperienceRepository = workExperienceRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<EmployerApplicantDto>>> Handle(GetApplicationsForJobAdQuery request, CancellationToken cancellationToken)
    {
        var jobAd = await _jobAdRepository.GetByIdAsync(request.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<PagedResult<EmployerApplicantDto>>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مورد نظر یافت نشد."));

        var ownershipCheck = await JobAdOwnershipGuard.EnsureOwnerAsync(jobAd, _companyRepository, _currentUser, cancellationToken);
        if (ownershipCheck.IsFailure)
            return Result.Failure<PagedResult<EmployerApplicantDto>>(ownershipCheck.Error);

        var paged = await _applicationRepository.GetByJobAdIdAsync(request.JobAdId, request.Page, request.PageSize, cancellationToken);

        var dtoItems = new List<EmployerApplicantDto>(paged.Items.Count);
        foreach (var application in paged.Items)
        {
            var candidate = await _candidateRepository.GetByIdAsync(application.CandidateId, cancellationToken);
            if (candidate is null)
                continue; // کارجوی حذف‌شده — نادیده گرفته می‌شود، نه خطا (سازگار با الگوی GetMyApplicationsQueryHandler)

            var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
            var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

            dtoItems.Add(CandidateMapper.ToEmployerDto(application, candidate, jobAd.Title, educations, workExperiences));
        }

        return Result.Success(PagedResult<EmployerApplicantDto>.Create(dtoItems, paged.TotalCount, paged.Page, paged.PageSize));
    }
}

/// <summary>
/// ویجت داشبورد شرکت «آخرین رزومه‌های دریافتی» — جدیدترین N متقاضی در میان همهٔ آگهی‌های شرکت
/// جاری، با هویت کامل کارجو (طبق همان تصمیم محصولی GetApplicationsForJobAdQuery). محدود به شرکتِ
/// کاربر واردشده — بدون امکان مرور آزاد بانک کارجویان.
/// </summary>
public sealed record GetRecentApplicantsForCompanyQuery(int Count = 5) : IRequest<Result<IReadOnlyList<EmployerApplicantDto>>>;

public sealed class GetRecentApplicantsForCompanyQueryHandler : IRequestHandler<GetRecentApplicantsForCompanyQuery, Result<IReadOnlyList<EmployerApplicantDto>>>
{
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly ICurrentUserService _currentUser;

    public GetRecentApplicantsForCompanyQueryHandler(
        IJobApplicationRepository applicationRepository, IJobAdRepository jobAdRepository,
        ICompanyRepository companyRepository, ICandidateRepository candidateRepository,
        ICandidateEducationRepository educationRepository, ICandidateWorkExperienceRepository workExperienceRepository,
        ICurrentUserService currentUser)
    {
        _applicationRepository = applicationRepository;
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _candidateRepository = candidateRepository;
        _educationRepository = educationRepository;
        _workExperienceRepository = workExperienceRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<EmployerApplicantDto>>> Handle(GetRecentApplicantsForCompanyQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<IReadOnlyList<EmployerApplicantDto>>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var company = await _companyRepository.GetByOwnerUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (company is null)
            return Result.Success<IReadOnlyList<EmployerApplicantDto>>(Array.Empty<EmployerApplicantDto>());

        var companyJobAds = await _jobAdRepository.GetAllByCompanyIdAsync(company.Id, cancellationToken);
        var jobAdsById = companyJobAds.ToDictionary(ad => ad.Id, ad => ad.Title);

        var allApplications = new List<JobApplication>();
        foreach (var jobAdId in jobAdsById.Keys)
            allApplications.AddRange(await _applicationRepository.GetAllByJobAdIdAsync(jobAdId, cancellationToken));

        var recent = allApplications.OrderByDescending(a => a.CreatedAtUtc).Take(request.Count);

        var result = new List<EmployerApplicantDto>();
        foreach (var application in recent)
        {
            var candidate = await _candidateRepository.GetByIdAsync(application.CandidateId, cancellationToken);
            if (candidate is null)
                continue;

            var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
            var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

            result.Add(CandidateMapper.ToEmployerDto(application, candidate, jobAdsById[application.JobAdId], educations, workExperiences));
        }

        return Result.Success<IReadOnlyList<EmployerApplicantDto>>(result);
    }
}

/// <summary>
/// دریافت اطلاعات لازم برای دانلود امن فایل رزومهٔ یک متقاضی توسط کارفرمای صاحب همان آگهی —
/// فقط مسیر نسبی فایل و نام پیشنهادی را برمی‌گرداند؛ resolve به مسیر فیزیکی و استریم واقعی فایل
/// در لایه Api (اندپوینت) از طریق IFileStorageService.ResolvePhysicalPath انجام می‌شود.
/// </summary>
public sealed record GetJobApplicationResumeQuery(Guid ApplicationId) : IRequest<Result<ResumeDownloadDto>>;

public sealed class GetJobApplicationResumeQueryHandler : IRequestHandler<GetJobApplicationResumeQuery, Result<ResumeDownloadDto>>
{
    private readonly IJobApplicationRepository _applicationRepository;
    private readonly IJobAdRepository _jobAdRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICurrentUserService _currentUser;

    public GetJobApplicationResumeQueryHandler(
        IJobApplicationRepository applicationRepository, IJobAdRepository jobAdRepository,
        ICompanyRepository companyRepository, ICandidateRepository candidateRepository, ICurrentUserService currentUser)
    {
        _applicationRepository = applicationRepository;
        _jobAdRepository = jobAdRepository;
        _companyRepository = companyRepository;
        _candidateRepository = candidateRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<ResumeDownloadDto>> Handle(GetJobApplicationResumeQuery request, CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result.Failure<ResumeDownloadDto>(Error.NotFound("APPLICATION_NOT_FOUND", "درخواست مورد نظر یافت نشد."));

        var jobAd = await _jobAdRepository.GetByIdAsync(application.JobAdId, cancellationToken);
        if (jobAd is null)
            return Result.Failure<ResumeDownloadDto>(Error.NotFound("JOB_AD_NOT_FOUND", "آگهی مرتبط یافت نشد."));

        var ownershipCheck = await JobAdOwnershipGuard.EnsureOwnerAsync(jobAd, _companyRepository, _currentUser, cancellationToken);
        if (ownershipCheck.IsFailure)
            return Result.Failure<ResumeDownloadDto>(ownershipCheck.Error);

        var candidate = await _candidateRepository.GetByIdAsync(application.CandidateId, cancellationToken);
        if (candidate is null)
            return Result.Failure<ResumeDownloadDto>(Error.NotFound("CANDIDATE_NOT_FOUND", "کارجوی مرتبط با این درخواست یافت نشد."));

        if (string.IsNullOrWhiteSpace(candidate.ResumeFileUrl))
            return Result.Failure<ResumeDownloadDto>(Error.NotFound("RESUME_FILE_NOT_FOUND", "فایل رزومه‌ای برای این متقاضی ثبت نشده است."));

        var extension = Path.GetExtension(candidate.ResumeFileUrl);
        var suggestedFileName = $"{candidate.FullName}-رزومه{extension}";

        return Result.Success(new ResumeDownloadDto(candidate.ResumeFileUrl, suggestedFileName));
    }
}
