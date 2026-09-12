using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Candidates;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>یک ردیف ورودی زبان در درخواست همگام‌سازی — بدون Id، تطبیق بر اساس Name انجام می‌شود.</summary>
public sealed record CandidateLanguageInput(string Name, string ProficiencyLevel);

/// <summary>
/// همگام‌سازی مجموعه زبان‌های کارجو — طبق فاز «پروفایل و رزومه‌ساز کارجو». برخلاف مهارت‌ها،
/// candidate_languages ایندکس یکتای فیلترشده روی (CandidateId, Name) دارد (ux_candidate_languages_candidate_id_name)
/// که به‌صورت فوری (نه Deferred) در پستگرس بررسی می‌شود؛ بنابراین رویکرد «حذف همه + ایجاد مجدد همه» می‌تواند
/// در همان تراکنش با این قید یکتا تداخل کند (بسته به ترتیب دستورات INSERT/UPDATE در EF Core). به همین دلیل
/// همگام‌سازی به‌صورت Diff-Based انجام می‌شود: ردیف‌هایی که نامشان در فهرست جدید باقی مانده فقط سطح تسلطشان
/// به‌روزرسانی می‌شود (بدون حذف/ایجاد مجدد)؛ فقط ردیف‌های واقعاً حذف‌شده Soft-Delete و فقط نام‌های واقعاً
/// جدید ایجاد می‌شوند.
/// </summary>
public sealed record UpdateCandidateLanguagesCommand(IReadOnlyList<CandidateLanguageInput> Languages) : IRequest<Result<CandidateDto>>;

public sealed class UpdateCandidateLanguagesCommandValidator : AbstractValidator<UpdateCandidateLanguagesCommand>
{
    public UpdateCandidateLanguagesCommandValidator()
    {
        RuleForEach(x => x.Languages).ChildRules(language =>
        {
            language.RuleFor(l => l.Name).NotEmpty().WithMessage("نام زبان الزامی است.").MaximumLength(100);
            language.RuleFor(l => l.ProficiencyLevel).IsEnumName(typeof(LanguageProficiencyLevel)).WithMessage("سطح تسلط نامعتبر است.");
        });

        // جلوگیری از ارسال نام‌های تکراری در یک درخواست — چون تطبیق Diff-Based بر اساس Name انجام می‌شود
        // و تکرار نام باعث ابهام در تشخیص «کدام ردیف باید به‌روزرسانی شود» می‌گردد.
        RuleFor(x => x.Languages)
            .Must(languages => languages.Select(l => l.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == languages.Count)
            .WithMessage("نام زبان‌ها نباید در یک درخواست تکراری باشد.");
    }
}

public sealed class UpdateCandidateLanguagesCommandHandler : IRequestHandler<UpdateCandidateLanguagesCommand, Result<CandidateDto>>
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

    public UpdateCandidateLanguagesCommandHandler(
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

    public async Task<Result<CandidateDto>> Handle(UpdateCandidateLanguagesCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CandidateDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var candidate = await _candidateRepository.GetByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (candidate is null)
            return Result.Failure<CandidateDto>(Error.NotFound("CANDIDATE_NOT_FOUND", "ابتدا باید پروفایل/رزومه خود را تکمیل کنید."));

        var existingLanguages = await _languageRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var existingByName = existingLanguages.ToDictionary(l => l.Name, StringComparer.OrdinalIgnoreCase);
        var requestedNames = new HashSet<string>(request.Languages.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);

        // حذف منطقی زبان‌هایی که در فهرست جدید دیگر وجود ندارند.
        foreach (var existingLanguage in existingLanguages)
        {
            if (!requestedNames.Contains(existingLanguage.Name))
                existingLanguage.MarkAsDeleted(_currentUser.UserId, _dateTimeProvider.UtcNow);
        }

        // به‌روزرسانی درجا برای زبان‌های باقی‌مانده و ایجاد برای زبان‌های واقعاً جدید.
        foreach (var languageInput in request.Languages)
        {
            var proficiencyLevel = Enum.Parse<LanguageProficiencyLevel>(languageInput.ProficiencyLevel);

            if (existingByName.TryGetValue(languageInput.Name, out var existingLanguage))
                existingLanguage.UpdateProficiencyLevel(proficiencyLevel);
            else
                _languageRepository.Add(CandidateLanguage.Create(candidate.Id, languageInput.Name, proficiencyLevel));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var educations = await _educationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var workExperiences = await _workExperienceRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var certifications = await _certificationRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var skills = await _skillRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);
        var languages = await _languageRepository.GetByCandidateIdAsync(candidate.Id, cancellationToken);

        return Result.Success(CandidateMapper.ToDto(candidate, educations, workExperiences, certifications, skills, languages));
    }
}
