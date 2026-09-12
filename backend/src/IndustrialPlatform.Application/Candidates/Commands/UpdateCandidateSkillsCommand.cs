using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>یک ردیف ورودی مهارت در درخواست همگام‌سازی — بدون Id چون کل مجموعه جایگزین می‌شود.</summary>
public sealed record CandidateSkillInput(string Name, string Category, int Level);

/// <summary>
/// همگام‌سازی/جایگزینی کامل مجموعه مهارت‌های ساختاریافته کارجو — طبق فاز «پروفایل و رزومه‌ساز کارجو»
/// (PUT، نه POST تکی: کل فهرست یک‌جا از فرم ویرایش ارسال می‌شود). چون روی candidate_skills هیچ ایندکس
/// یکتایی تعریف نشده (برخلاف candidate_languages)، ساده‌ترین و امن‌ترین پیاده‌سازی «حذف منطقی همه ردیف‌های
/// قبلی + افزودن ردیف‌های جدید» است — بدون نگرانی از تداخل با قید یکتا در همان تراکنش.
/// </summary>
public sealed record UpdateCandidateSkillsCommand(IReadOnlyList<CandidateSkillInput> Skills) : IRequest<Result<CandidateDto>>;

public sealed class UpdateCandidateSkillsCommandValidator : AbstractValidator<UpdateCandidateSkillsCommand>
{
    public UpdateCandidateSkillsCommandValidator()
    {
        RuleForEach(x => x.Skills).ChildRules(skill =>
        {
            skill.RuleFor(s => s.Name).NotEmpty().WithMessage("نام مهارت الزامی است.").MaximumLength(100);
            skill.RuleFor(s => s.Category).NotEmpty().WithMessage("دسته‌بندی مهارت الزامی است.").MaximumLength(50);
            skill.RuleFor(s => s.Level).InclusiveBetween(1, 5).WithMessage("سطح تسلط باید بین ۱ تا ۵ باشد.");
        });
    }
}

public sealed class UpdateCandidateSkillsCommandHandler : IRequestHandler<UpdateCandidateSkillsCommand, Result<CandidateDto>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly ICandidateCertificationRepository _certificationRepository;
    private readonly ICandidateSkillRepository _skillRepository;
    private readonly ICandidateLanguageRepository _languageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateCandidateSkillsCommandHandler(
        ICandidateRepository candidateRepository,
        ICandidateEducationRepository educationRepository,
        ICandidateWorkExperienceRepository workExperienceRepository,
        ICandidateCertificationRepository certificationRepository,
        ICandidateSkillRepository skillRepository,
        ICandidateLanguageRepository languageRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _candidateRepository = candidateRepository;
        _educationRepository = educationRepository;
        _workExperienceRepository = workExperienceRepository;
        _certificationRepository = certificationRepository;
        _skillRepository = skillRepository;
        _languageRepository = languageRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CandidateDto>> Handle(UpdateCandidateSkillsCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CandidateDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Failure<CandidateDto>(Error.NotFound("CANDIDATE_NOT_FOUND", "ابتدا باید پروفایل/رزومه خود را تکمیل کنید."));

        var existingSkills = await _skillRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        foreach (var existingSkill in existingSkills)
            existingSkill.MarkAsDeleted(_currentUser.UserId, _dateTimeProvider.UtcNow);

        foreach (var skillInput in request.Skills)
            _skillRepository.Add(CandidateSkill.Create(candidate.Id, skillInput.Name, skillInput.Category, skillInput.Level));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var certifications = await _certificationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var skills = await _skillRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var languages = await _languageRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        return Result.Success(CandidateMapper.ToDto(candidate, educations, workExperiences, certifications, skills, languages));
    }
}
