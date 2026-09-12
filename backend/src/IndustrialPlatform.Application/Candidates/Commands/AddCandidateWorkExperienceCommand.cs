using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// افزودن یک ردیف سابقه شغلی ساختاریافته به رزومه‌ساز — طبق فاز «مدیریت رزومه‌ها و متقاضیان» (چند
/// ردیفی با بازه زمانی، مکمل فیلد آزاد Candidate.WorkExperienceSummary، نه جایگزین آن). EndYear=null
/// به معنای «اکنون/همچنان مشغول به کار» است. فقط برای کارجوی صاحب پروفایل جاری.
/// </summary>
public sealed record AddCandidateWorkExperienceCommand(
    string JobTitle,
    string CompanyName,
    int StartYear,
    int? EndYear = null,
    string? Description = null) : IRequest<Result<CandidateDto>>;

public sealed class AddCandidateWorkExperienceCommandValidator : AbstractValidator<AddCandidateWorkExperienceCommand>
{
    public AddCandidateWorkExperienceCommandValidator()
    {
        RuleFor(x => x.JobTitle).NotEmpty().WithMessage("عنوان شغلی الزامی است.").MaximumLength(150);
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("نام شرکت/کارفرما الزامی است.").MaximumLength(200);
        RuleFor(x => x.StartYear).InclusiveBetween(1300, 1500).WithMessage("سال شروع نامعتبر است.");
        RuleFor(x => x.EndYear).InclusiveBetween(1300, 1500).WithMessage("سال پایان نامعتبر است.").When(x => x.EndYear.HasValue);
        RuleFor(x => x)
            .Must(x => !x.EndYear.HasValue || x.EndYear.Value >= x.StartYear)
            .WithMessage("سال پایان نمی‌تواند قبل از سال شروع باشد.")
            .WithName("EndYear");
    }
}

public sealed class AddCandidateWorkExperienceCommandHandler : IRequestHandler<AddCandidateWorkExperienceCommand, Result<CandidateDto>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly ICandidateCertificationRepository _certificationRepository;
    private readonly ICandidateSkillRepository _skillRepository;
    private readonly ICandidateLanguageRepository _languageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AddCandidateWorkExperienceCommandHandler(
        ICandidateRepository candidateRepository,
        ICandidateEducationRepository educationRepository,
        ICandidateWorkExperienceRepository workExperienceRepository,
        ICandidateCertificationRepository certificationRepository,
        ICandidateSkillRepository skillRepository,
        ICandidateLanguageRepository languageRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _candidateRepository = candidateRepository;
        _educationRepository = educationRepository;
        _workExperienceRepository = workExperienceRepository;
        _certificationRepository = certificationRepository;
        _skillRepository = skillRepository;
        _languageRepository = languageRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CandidateDto>> Handle(AddCandidateWorkExperienceCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CandidateDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Failure<CandidateDto>(Error.NotFound("CANDIDATE_NOT_FOUND", "ابتدا باید پروفایل/رزومه خود را تکمیل کنید."));

        var workExperience = CandidateWorkExperience.Create(
            candidate.Id, request.JobTitle, request.CompanyName, request.StartYear, request.EndYear, request.Description);
        _workExperienceRepository.Add(workExperience);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var certifications = await _certificationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var skills = await _skillRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var languages = await _languageRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        return Result.Success(CandidateMapper.ToDto(candidate, educations, workExperiences, certifications, skills, languages));
    }
}
