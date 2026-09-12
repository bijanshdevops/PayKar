using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

public sealed record SaveCandidateProfileCommand(
    string FullName,
    string MilitaryServiceStatus,
    string EducationLevel,
    string? WorkExperienceSummary,
    string? Skills,
    string? Interests = null,
    string? PsychologyAnswers = null,
    string? Email = null,
    string? City = null,
    // ---------- طبق فاز «پروفایل و رزومه‌ساز کارجو» ----------
    // یادداشت: ترجیحات شغلی (نوع همکاری/محدوده حقوق/وضعیت جستجوی کار) عمداً اینجا نیست — طبق
    // UpdateCandidateJobPreferencesCommand مجزا ذخیره می‌شود تا فرم «ترجیحات شغلی» مستقل از این فرم باشد.
    string? JobTitle = null,
    string? ProfessionalSummary = null,
    string? LinkedInUrl = null,
    string? GitHubUrl = null,
    string? PersonalWebsiteUrl = null) : IRequest<Result<CandidateDto>>;

public sealed class SaveCandidateProfileCommandValidator : AbstractValidator<SaveCandidateProfileCommand>
{
    public SaveCandidateProfileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("نام و نام خانوادگی الزامی است.");
        RuleFor(x => x.MilitaryServiceStatus).IsEnumName(typeof(Domain.Candidates.MilitaryServiceStatus));
        RuleFor(x => x.EducationLevel).NotEmpty().WithMessage("آخرین مدرک تحصیلی الزامی است.");
        // ایمیل کاملاً اختیاری است؛ فقط وقتی مقدار دارد باید فرمت معتبر باشد.
        RuleFor(x => x.Email).EmailAddress().WithMessage("فرمت ایمیل نامعتبر است.").When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.JobTitle).MaximumLength(150);
        RuleFor(x => x.LinkedInUrl).MaximumLength(300).Must(BeAValidHttpUrl).WithMessage("لینک لینکدین نامعتبر است.").When(x => !string.IsNullOrWhiteSpace(x.LinkedInUrl));
        RuleFor(x => x.GitHubUrl).MaximumLength(300).Must(BeAValidHttpUrl).WithMessage("لینک گیت‌هاب نامعتبر است.").When(x => !string.IsNullOrWhiteSpace(x.GitHubUrl));
        RuleFor(x => x.PersonalWebsiteUrl).MaximumLength(300).Must(BeAValidHttpUrl).WithMessage("لینک وب‌سایت شخصی نامعتبر است.").When(x => !string.IsNullOrWhiteSpace(x.PersonalWebsiteUrl));
    }

    private static bool BeAValidHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var parsed) && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
}

/// <summary>
/// ایجاد یا به‌روزرسانی پروفایل/رزومه کارجو — یک کاربر با نقش Candidate دقیقاً یک پروفایل دارد.
/// </summary>
public sealed class SaveCandidateProfileCommandHandler : IRequestHandler<SaveCandidateProfileCommand, Result<CandidateDto>>
{
    private readonly ICandidateRepository _repository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly ICandidateCertificationRepository _certificationRepository;
    private readonly ICandidateSkillRepository _skillRepository;
    private readonly ICandidateLanguageRepository _languageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SaveCandidateProfileCommandHandler(
        ICandidateRepository repository,
        ICandidateEducationRepository educationRepository,
        ICandidateWorkExperienceRepository workExperienceRepository,
        ICandidateCertificationRepository certificationRepository,
        ICandidateSkillRepository skillRepository,
        ICandidateLanguageRepository languageRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _educationRepository = educationRepository;
        _workExperienceRepository = workExperienceRepository;
        _certificationRepository = certificationRepository;
        _skillRepository = skillRepository;
        _languageRepository = languageRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CandidateDto>> Handle(SaveCandidateProfileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CandidateDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var militaryStatus = Enum.Parse<Domain.Candidates.MilitaryServiceStatus>(request.MilitaryServiceStatus);
        var existing = await _repository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);

        if (existing is not null)
        {
            // ترجیحات شغلی (PreferredWorkType/محدوده حقوق/وضعیت جستجو) عمداً اینجا دست‌نخورده باقی می‌ماند
            // چون از طریق UpdateCandidateJobPreferencesCommand مجزا مدیریت می‌شود؛ مقادیر فعلی آن‌ها حفظ می‌شوند.
            var updateResult = existing.UpdateProfile(
                request.FullName, militaryStatus, request.EducationLevel, request.WorkExperienceSummary, request.Skills,
                request.Interests, request.PsychologyAnswers, request.Email, request.City, request.JobTitle,
                request.ProfessionalSummary, request.LinkedInUrl, request.GitHubUrl, request.PersonalWebsiteUrl,
                existing.PreferredWorkType, existing.MinRequestedSalaryInToman, existing.MaxRequestedSalaryInToman,
                existing.IsActivelyLookingForJob);

            if (updateResult.IsFailure)
                return Result.Failure<CandidateDto>(updateResult.Error);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(await BuildDtoAsync(existing, cancellationToken));
        }

        var createResult = Candidate.Create(
            _currentUser.UserId.Value, _currentUser.MobileNumber ?? string.Empty, request.FullName, militaryStatus,
            request.EducationLevel, request.WorkExperienceSummary, request.Skills, request.Interests, request.PsychologyAnswers,
            request.Email, request.City, request.JobTitle, request.ProfessionalSummary, request.LinkedInUrl, request.GitHubUrl,
            request.PersonalWebsiteUrl);

        if (createResult.IsFailure)
            return Result.Failure<CandidateDto>(createResult.Error);

        _repository.Add(createResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CandidateMapper.ToDto(
            createResult.Value, Array.Empty<CandidateEducation>(), Array.Empty<CandidateWorkExperience>(),
            Array.Empty<CandidateCertification>(), Array.Empty<CandidateSkill>(), Array.Empty<CandidateLanguage>()));
    }

    private async Task<CandidateDto> BuildDtoAsync(Candidate candidate, CancellationToken cancellationToken)
    {
        var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var certifications = await _certificationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var skills = await _skillRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var languages = await _languageRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        return CandidateMapper.ToDto(candidate, educations, workExperiences, certifications, skills, languages);
    }
}
